using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using TableInterface;
using TableInterface.Entities;

namespace Functions
{
    public static class UpdateTokenAllocations
    {
        [FunctionName("UpdateTokenAllocations")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequestMessage req, TraceWriter log)
        {
            Startup.Init();

            TokenAllocationTableAdapter tokenAllocationTableAdapter = new TokenAllocationTableAdapter();
            tokenAllocationTableAdapter.Init();

            List<FrequencyEntity> entities = tokenAllocationTableAdapter.GetAllCountGte(TokenAllocationTableAdapter.PartitionLabel, 8)
                .Where(x => x.Ignore == false).OrderByDescending(x => x.Count).ToList();

            int nextIndex = 1;
            if (entities.Any(x => x.AllocatedIndex.HasValue))
            {
                nextIndex = entities.Max(x => x.AllocatedIndex.Value) + 1;
            }

            List<FrequencyEntity> updatedEntities = new List<FrequencyEntity>();
            int labelAllocatedCount = 0;
            foreach (FrequencyEntity frequencyEntity in entities)
            {
                if (!frequencyEntity.AllocatedIndex.HasValue)
                {
                    frequencyEntity.AllocatedIndex = nextIndex++;
                    labelAllocatedCount++;
                    updatedEntities.Add(frequencyEntity);
                }
            }

            tokenAllocationTableAdapter.Update(updatedEntities);

            entities = tokenAllocationTableAdapter.GetAllCountGte(TokenAllocationTableAdapter.PartitionDigram, 7)
                .Where(x => x.Ignore == false).OrderByDescending(x => x.Count).ToList();

            nextIndex = 1;
            if (entities.Any(x => x.AllocatedIndex.HasValue))
            {
                nextIndex = entities.Max(x => x.AllocatedIndex.Value) + 1;
            }

            updatedEntities = new List<FrequencyEntity>();
            int digramAllocatedCount = 0;
            foreach (FrequencyEntity frequencyEntity in entities)
            {
                if (!frequencyEntity.AllocatedIndex.HasValue)
                {
                    frequencyEntity.AllocatedIndex = nextIndex++;
                    digramAllocatedCount++;
                    updatedEntities.Add(frequencyEntity);
                }
            }

            tokenAllocationTableAdapter.Update(updatedEntities);

            return req.CreateResponse(HttpStatusCode.OK, $"Allocated {labelAllocatedCount} label indexes and {digramAllocatedCount} digram indexes");
        }
    }
}