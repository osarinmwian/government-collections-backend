using GovernmentCollections.Domain.Enums;
using GovernmentCollections.Domain.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GovernmentCollections.Service.Gateways;

public interface IPaymentGatewayFactory
{
    IPaymentGateway GetGateway(PaymentGateway gateway);
}

public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<PaymentGateway, Func<IPaymentGateway>> _gateways;

    public PaymentGatewayFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _gateways = new Dictionary<PaymentGateway, Func<IPaymentGateway>>
        {
            { PaymentGateway.RevPay, () => CreateRevPayGateway() },
            { PaymentGateway.Remita, () => CreateRemitaGateway() },
            { PaymentGateway.Interswitch, () => CreateInterswitchGateway() },
            { PaymentGateway.BuyPower, () => CreateBuyPowerGateway() }
        };
    }

    public IPaymentGateway GetGateway(PaymentGateway gateway)
    {
        if (_gateways.ContainsKey(gateway))
        {
            return _gateways[gateway]();
        }
        
        throw new NotSupportedException($"Gateway {gateway} is not supported");
    }

    private RevPayGateway CreateRevPayGateway()
    {
        var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        var settings = _serviceProvider.GetRequiredService<RevPaySettings>();
        var logger = _serviceProvider.GetRequiredService<ILogger<RevPayGateway>>();
        return new RevPayGateway(httpClient, settings, logger);
    }

    private RemitaGateway CreateRemitaGateway()
    {
        var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        var settings = _serviceProvider.GetRequiredService<RemitaSettings>();
        var logger = _serviceProvider.GetRequiredService<ILogger<RemitaGateway>>();
        return new RemitaGateway(httpClient, settings, logger);
    }

    private InterswitchGovernmentCollectionsGateway CreateInterswitchGateway()
    {
        var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        var settings = _serviceProvider.GetRequiredService<InterswitchSettings>();
        var logger = _serviceProvider.GetRequiredService<ILogger<InterswitchGovernmentCollectionsGateway>>();
        return new InterswitchGovernmentCollectionsGateway(httpClient, settings, logger);
    }

    private BuyPowerGateway CreateBuyPowerGateway()
    {
        var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        var settings = _serviceProvider.GetRequiredService<BuyPowerSettings>();
        var logger = _serviceProvider.GetRequiredService<ILogger<BuyPowerGateway>>();
        return new BuyPowerGateway(httpClient, settings, logger);
    }
}