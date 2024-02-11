using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace UFX.Orleans.SignalRBackplane.Grains;

internal class GrainInterfaceTypeProvider(IOptions<SignalrOrleansOptions> options) : IGrainInterfaceTypeProvider
{
    public bool TryGetGrainInterfaceType(Type type, out GrainInterfaceType grainInterfaceType)
    {
        var useFullyQualifiedGrainType = options.Value.UseFullyQualifiedGrainTypes
                                         && type.FullName is not null
                                         && typeof(IGrain).IsAssignableFrom(type)
                                         && type.Assembly.FullName!.StartsWith("UFX.Orleans.SignalRBackplane");

        grainInterfaceType = useFullyQualifiedGrainType ? GrainInterfaceType.Create(type.FullName!) : default;
        return useFullyQualifiedGrainType;
    }
}