using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Calamari.Kubernetes.ResourceStatus.Resources;

/// <summary>
/// Represents a kubernetes resource in a cluster, including its status
/// </summary>
public class Resource
{
    [JsonIgnore]
    public JObject Data { get; }
    
    [JsonIgnore]
    public IEnumerable<string> OwnerUids { get; }

    public string Uid { get; }
    public string Kind { get; }
    public string Name { get; }
    public string Namespace { get; }
    
    // TODO what's a good default?
    [JsonConverter(typeof(StringEnumConverter))]
    public virtual ResourceStatus Status => ResourceStatus.Successful;

    [JsonIgnore]
    public virtual string ChildKind => "";

    public Resource(JObject json)
    {
        Data = json;
        OwnerUids = Data.SelectTokens("$.metadata.ownerReferences[*].uid").Values<string>();
        Uid = Field("$.metadata.uid");
        Kind = Field("$.kind");
        Name = Field("$.metadata.name");
        Namespace = Field("$.metadata.namespace");
    }

    public virtual bool HasUpdate(Resource lastStatus) => false;

    protected virtual void PopulateData(JObject data)
    {
    }
    
    protected string Field(string jsonPath) => FieldOrDefault(jsonPath, "");
    
    protected T FieldOrDefault<T>(string jsonPath, T defaultValue)
    {
        var result = Data.SelectToken(jsonPath);
        return result == null ? defaultValue : result.Value<T>();
    }

    protected static T CastOrThrow<T>(Resource resource) where T: Resource
    {
        if (resource is not T subType)
        {
            throw new Exception($"Cannot cast resource to subtype {nameof(T)}");
        }

        return subType;
    }
}