using Blockcore.Networks;

public static class Networks
{
    public static NetworksSelector Bitcoin
    {
        get
        {
            return new NetworksSelector(
                () => null,
                () => new BitcoinTest(),
                () => null, // Replace with RegTest if needed
                () => null, // Add other networks if needed
                () => null,
                () => null,
                () => null,
                () => null
            );
        }
    }
}

public class NetworksSelector
{
    public NetworksSelector(
        Func<Network> mainnet,
        Func<Network> testnet,
        Func<Network> regtest,
        Func<Network> testnet4,
        Func<Network> signet,
        Func<Network> angornet,
        Func<Network> mutinynet,
        Func<Network> liquidnet)
    {
        this.Mainnet = mainnet;
        this.Testnet = testnet;
        this.Regtest = regtest;
        this.Testnet4 = testnet4;
        this.Signet = signet;
        this.Angornet = angornet;
        this.Mutinynet = mutinynet;
        this.Liquidnet = liquidnet;
    }

    public Func<Network> Mainnet { get; }
    public Func<Network> Testnet { get; }
    public Func<Network> Testnet4 { get; }
    public Func<Network> Regtest { get; }
    public Func<Network> Signet { get; }
    public Func<Network> Angornet { get; }
    public Func<Network> Mutinynet { get; }
    public Func<Network> Liquidnet { get; }
}