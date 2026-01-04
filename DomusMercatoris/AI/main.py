import argparse
import os
import sys
import json
import pyodbc
import torch
import torch.nn as nn
from torchvision import models, transforms
from PIL import Image
from sklearn.cluster import KMeans
import numpy as np

# --- Configuration ---
DB_CONNECTION_STRING = os.environ.get('DB_CONNECTION_STRING')
if not DB_CONNECTION_STRING:
    # Fallback or error. For now, assuming it's passed.
    # Note: On macOS, pyodbc requires a driver (e.g., 'ODBC Driver 17 for SQL Server')
    # If using Docker or specific setup, adjust accordingly.
    # We might need to parse the EF Core connection string to pyodbc format if it differs significantly.
    pass

def get_db_connection():
    if not DB_CONNECTION_STRING:
        print("Error: DB_CONNECTION_STRING environment variable not set.")
        sys.exit(1)
    
    # EF Core connection string might look like: "Server=localhost;Database=DomusDB;User Id=sa;Password=...;"
    # pyodbc expects: "DRIVER={ODBC Driver 17 for SQL Server};SERVER=localhost;DATABASE=DomusDB;UID=sa;PWD=...;"
    # We'll try to adapt it simply.
    conn_str = DB_CONNECTION_STRING
    if "DRIVER=" not in conn_str.upper():
        # Heuristic to add driver if missing (MacOS/Linux usually needs explicit driver)
        # Check installed drivers: /usr/local/etc/odbcinst.ini or similar
        # For now, let's assume a standard driver is available.
        # Common macOS driver: "ODBC Driver 17 for SQL Server"
        conn_str = "DRIVER={ODBC Driver 17 for SQL Server};" + conn_str
    
    return pyodbc.connect(conn_str)

# --- Feature Extraction ---
def get_resnet_model():
    # Load ResNet50 with default weights (ImageNet)
    model = models.resnet50(weights=models.ResNet50_Weights.DEFAULT)
    # Remove the last classification layer (fc) to get features (2048-dim)
    # ResNet50 structure: ... -> avgpool -> fc
    # We want output of avgpool.
    # Option 1: Use create_feature_extractor (newer torchvision)
    # Option 2: Replace fc with Identity (easier)
    model.fc = nn.Identity()
    model.eval()
    return model

def extract_features(model, image_paths):
    preprocess = transforms.Compose([
        transforms.Resize(256),
        transforms.CenterCrop(224),
        transforms.ToTensor(),
        transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
    ])

    vectors = []
    for img_path in image_paths:
        try:
            input_image = Image.open(img_path).convert('RGB')
            input_tensor = preprocess(input_image)
            input_batch = input_tensor.unsqueeze(0) # create a mini-batch as expected by the model

            with torch.no_grad():
                output = model(input_batch)
            
            # Output is [1, 2048]
            vectors.append(output[0].numpy())
        except Exception as e:
            print(f"Warning: Failed to process image {img_path}: {e}")

    if not vectors:
        return None
    
    # Average the vectors if multiple images
    avg_vector = np.mean(vectors, axis=0)
    return avg_vector.tolist()

def handle_extract(args):
    print(f"Extracting features for Product ID: {args.product_id}")
    model = get_resnet_model()
    
    features = extract_features(model, args.images)
    
    if features:
        feature_json = json.dumps(features)
        
        try:
            conn = get_db_connection()
            cursor = conn.cursor()
            
            # Check if exists
            cursor.execute("SELECT Id FROM ProductFeatures WHERE ProductId = ?", args.product_id)
            row = cursor.fetchone()
            
            import datetime
            now = datetime.datetime.utcnow()

            if row:
                cursor.execute("UPDATE ProductFeatures SET FeatureVectorJson = ?, CreatedAt = ? WHERE ProductId = ?", feature_json, now, args.product_id)
            else:
                cursor.execute("INSERT INTO ProductFeatures (ProductId, FeatureVectorJson, CreatedAt) VALUES (?, ?, ?)", args.product_id, feature_json, now)
            
            conn.commit()
            print("Features saved successfully.")
        except Exception as e:
            print(f"Database error: {e}")
            sys.exit(1)
    else:
        print("No features extracted.")

# --- Clustering ---
def handle_cluster(args):
    print(f"Running K-Means Clustering with K={args.k}")
    
    try:
        conn = get_db_connection()
        cursor = conn.cursor()
        
        # 1. Fetch all features
        cursor.execute("SELECT ProductId, FeatureVectorJson FROM ProductFeatures")
        rows = cursor.fetchall()
        
        if not rows:
            print("No product features found.")
            return

        product_ids = []
        data_matrix = []
        
        for row in rows:
            pid = row[0]
            vec_json = row[1]
            try:
                vec = json.loads(vec_json)
                if len(vec) == 2048:
                    product_ids.append(pid)
                    data_matrix.append(vec)
            except:
                pass
        
        if len(data_matrix) < args.k:
            print(f"Not enough data points ({len(data_matrix)}) for {args.k} clusters.")
            # Adjust K or exit
            args.k = max(1, len(data_matrix))

        # 2. Run KMeans
        X = np.array(data_matrix)
        kmeans = KMeans(n_clusters=args.k, random_state=42, n_init=10)
        kmeans.fit(X)
        labels = kmeans.labels_
        
        # 3. Save Results
        # Get max version
        cursor.execute("SELECT MAX(Version) FROM ProductClusters")
        res = cursor.fetchone()
        current_max_version = res[0] if res[0] is not None else 0
        new_version = current_max_version + 1
        
        import datetime
        now = datetime.datetime.utcnow()
        
        # Create Clusters
        cluster_id_map = {} # label_idx -> db_id
        
        for i in range(args.k):
            cluster_name = f"Cluster {i+1} (v{new_version})"
            cursor.execute("INSERT INTO ProductClusters (Name, Version, CreatedAt) OUTPUT INSERTED.Id VALUES (?, ?, ?)", cluster_name, new_version, now)
            cluster_db_id = cursor.fetchone()[0]
            cluster_id_map[i] = cluster_db_id
        
        # Assign Members
        for idx, pid in enumerate(product_ids):
            label = labels[idx]
            cluster_db_id = cluster_id_map[label]
            cursor.execute("INSERT INTO ProductClusterMembers (ProductClusterId, ProductId) VALUES (?, ?)", cluster_db_id, pid)
            
        conn.commit()
        print(f"Clustering completed. Version {new_version} created.")
        
    except Exception as e:
        print(f"Error during clustering: {e}")
        sys.exit(1)

# --- Main ---
if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Domus Mercatoris AI Service")
    subparsers = parser.add_subparsers(dest="command", required=True)
    
    # Extract Command
    parser_extract = subparsers.add_parser("extract", help="Extract features from images")
    parser_extract.add_argument("--product_id", type=int, required=True, help="Product ID")
    parser_extract.add_argument("--images", nargs="+", required=True, help="List of image paths")
    
    # Cluster Command
    parser_cluster = subparsers.add_parser("cluster", help="Run clustering")
    parser_cluster.add_argument("--k", type=int, required=True, help="Number of clusters")
    
    args = parser.parse_args()
    
    if args.command == "extract":
        handle_extract(args)
    elif args.command == "cluster":
        handle_cluster(args)
