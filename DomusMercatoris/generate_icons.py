from PIL import Image
import os

source_path = '/Users/ahmetdemircan/Desktop/Solidus/DomusMercatoris/MVC/MVC/wwwroot/icon.png'
wwwroot = '/Users/ahmetdemircan/Desktop/Solidus/DomusMercatoris/MVC/MVC/wwwroot'

def generate_icon(source_path, output_path, size):
    img = Image.open(source_path)
    # Create a new square image with transparent background
    new_img = Image.new("RGBA", size, (0, 0, 0, 0))
    
    # Calculate resizing while maintaining aspect ratio
    img.thumbnail(size, Image.Resampling.LANCZOS)
    
    # Calculate position to center the image
    x = (size[0] - img.width) // 2
    y = (size[1] - img.height) // 2
    
    new_img.paste(img, (x, y))
    new_img.save(output_path)
    print(f"Generated {output_path}")

# Generate favicon.png (192x192)
generate_icon(source_path, os.path.join(wwwroot, 'favicon.png'), (192, 192))

# Generate apple-touch-icon.png (180x180) - iOS often fills background black or white if transparent, but let's stick to transparent or maybe fill with white if needed. 
# For now, keeping transparency is standard for PNGs, though iOS might force black background if transparency exists.
# Let's add a white background for apple-touch-icon just in case, as iOS icons don't support transparency well (they turn black).
# Actually, let's stick to the source. If the user wants a background, they usually provide it. 
# But to be safe for "icon", I'll just use the same logic.
generate_icon(source_path, os.path.join(wwwroot, 'apple-touch-icon.png'), (180, 180))

# Generate favicon.ico (32x32)
img = Image.open(source_path)
img.thumbnail((32, 32), Image.Resampling.LANCZOS)
new_img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
x = (32 - img.width) // 2
y = (32 - img.height) // 2
new_img.paste(img, (x, y))
new_img.save(os.path.join(wwwroot, 'favicon.ico'))
print(f"Generated {os.path.join(wwwroot, 'favicon.ico')}")

