using System;
using Newtonsoft.Json.Linq;

namespace Calamari.Kubernetes.ResourceStatus.Resources
{
    public class Pod : Resource
    {
        public string Phase { get; }
        public override ResourceStatus Status { get; }
    
        public Pod(JObject json) : base(json)
        {
            Phase = Field("$.status.phase");
            
            // TODO implement this
            switch (Phase)
            {
                case"Succeeded": 
                case "Running":
                    Status = ResourceStatus.Successful;
                    break;
                case "Pending":
                    Status = ResourceStatus.InProgress;
                    break;
                default:
                    Status = ResourceStatus.Failed;
                    break;
            }
        }
    
        public override bool HasUpdate(Resource lastStatus)
        {
            var last = CastOrThrow<Pod>(lastStatus);
            return last.Phase != Phase;
        }
    }
}