using OpenFeature.Error;
using OpenFeature.Model;
using Statsig;

namespace OpenFeature.Contrib.Providers.Statsig;

internal static class EvaluationContextExtensions
{
    //These keys match the keys of the statsiguser object as described here
    //https://docs.statsig.com/client/concepts/user
    internal const string CONTEXT_APP_VERSION = "appVersion";
    internal const string CONTEXT_COUNTRY = "country";
    internal const string CONTEXT_EMAIL = "email";
    internal const string CONTEXT_IP = "ip";
    internal const string CONTEXT_LOCALE = "locale";
    internal const string CONTEXT_USER_AGENT = "userAgent";
    internal const string CONTEXT_PRIVATE_ATTRIBUTES = "privateAttributes";
    internal const string CONTEXT_CUSTOM_IDS = "customIDs";

    public static StatsigUser AsStatsigUser(this EvaluationContext evaluationContext)
    {
        if (evaluationContext == null)
            return null;

        var user = new StatsigUser() { UserID = evaluationContext.TargetingKey };
        foreach (var item in evaluationContext)
        {
            switch (item.Key)
            {
                case CONTEXT_APP_VERSION:
                    user.AppVersion = item.Value.AsString;
                    break;
                case CONTEXT_COUNTRY:
                    user.Country = item.Value.AsString;
                    break;
                case CONTEXT_EMAIL:
                    user.Email = item.Value.AsString;
                    break;
                case CONTEXT_IP:
                    user.IPAddress = item.Value.AsString;
                    break;
                case CONTEXT_USER_AGENT:
                    user.UserAgent = item.Value.AsString;
                    break;
                case CONTEXT_LOCALE:
                    user.Locale = item.Value.AsString;
                    break;
                case CONTEXT_PRIVATE_ATTRIBUTES:
                    if (item.Value.IsStructure)
                    {
                        var privateAttributes = item.Value.AsStructure;
                        foreach (var items in privateAttributes)
                        {
                            user.AddPrivateAttribute(items.Key, items.Value.AsObject);
                        }
                    }
                    break;
                case CONTEXT_CUSTOM_IDS:
                    if (item.Value.IsStructure)
                    {
                        var customIds = item.Value.AsStructure;
                        foreach (var customId in customIds)
                        {
                            if (customId.Value.IsString)
                                user.AddCustomID(customId.Key, customId.Value.AsString);
                            else throw new FeatureProviderException(Constant.ErrorType.TypeMismatch, "Only string values are supported for CustomIDs");
                        }
                    }
                    break;

                default:
                    user.AddCustomProperty(item.Key, item.Value.AsObject);
                    break;
            }
        }
        return user;
    }
}
