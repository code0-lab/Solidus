using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using DomusMercatoris.Core.Entities;

namespace DomusMercatoris.Service.Interfaces
{
    public interface IClusteringService
    {
        Task ExtractAndStoreFeaturesAsync(long productId, List<string> imagePaths);
        Task RunClusteringAsync(int numberOfClusters);
        Task ProcessAllProductsFeaturesAsync();
        Task<List<float>?> ExtractFeaturesFromFilesAsync(List<IFormFile> files);
        Task<ProductCluster?> FindNearestClusterAsync(List<float> featureVector, double minSimilarity = 0.60);
        Task SplitClusterAsync(int clusterId, int numberOfSubClusters = 2);
        Task<ProductClusterMember?> GetClusterMemberByProductIdAsync(long productId);
        Task<List<long>> GetSimilarProductsAsync(long clusterId, List<float> targetVector, int? companyId, int take = 10);
    }
}
