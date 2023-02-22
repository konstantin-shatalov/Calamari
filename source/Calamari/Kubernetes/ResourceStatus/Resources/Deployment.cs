using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Calamari.Kubernetes.ResourceStatus.Resources;

public class Deployment : Resource
{
    public override string ChildKind => "ReplicaSet";
    
    public int Replicas { get; }
    public int UpToDate { get; }
    public int Ready { get; }
    public int Available { get; }
    public override ResourceStatus Status { get; }
    
    public Deployment(JObject json) : base(json)
    {
        Replicas = FieldOrDefault("$.status.replicas", 0);
        Available = FieldOrDefault("$.status.availableReplicas", 0);
        Ready = FieldOrDefault("$.status.readyReplicas", 0);
        UpToDate = FieldOrDefault("$.status.updatedReplicas", 0);
        
        Status = ResourceStatus.Failed;
    }

    public override bool HasUpdate(Resource lastStatus)
    {
        var last = CastOrThrow<Deployment>(lastStatus);
        return last.Replicas != Replicas 
               || last.UpToDate != UpToDate 
               || last.Ready != Ready 
               || last.Available != Available;
    }
}