using Microsoft.Extensions.Options;
using Orleans.Metadata;
using Orleans.Runtime;

namespace UFX.Orleans.SignalRBackplane.Grains;

internal class GrainTypeProvider(IOptions<SignalrOrleansOptions> options): IGrainTypeProvider
{
    public bool TryGetGrainType(Type type, out GrainType grainType)
    {
        var useFullyQualifiedGrainType = options.Value.UseFullyQualifiedGrainTypes
                                         && type.FullName is not null
                                         && typeof(SignalrBaseGrain).IsAssignableFrom(type)
                                         && type.Assembly.FullName!.StartsWith("UFX.Orleans.SignalRBackplane");

        grainType = useFullyQualifiedGrainType ? GrainType.Create(type.FullName!) : default;
        return useFullyQualifiedGrainType;
    }
}