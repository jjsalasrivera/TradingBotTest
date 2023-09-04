namespace TradingBot.libCommon
{
	public record Configuration(string Symbol, BinanceClientConf BinanceClientConf );

    public record BinanceClientConf(string APIKey, string APIPass);   
}
