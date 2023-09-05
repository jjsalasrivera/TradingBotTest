# TradingBotTest
This is a Test Project to check your own trading strategies. The functionallity its simple, get historic candles of a currency and pass candle by candle to your strategy class.

## How to implement a new strategy

You just only must to implemte the interface IStrategy in libStrategies project.

```C#
public interface IStrategy
{
    public Order CheckForOperation(Position position);

    public void AddCandleToHistory( Candle candle );
}
```

When a new candle arrives to your application, add to your strategy class with 
```C# 
AddCandleToHistory( Candle candle );
```
Then, Check if you need to perform an operation of buy, sell, stoploss or nothing calling to
```C#
Order CheckForOperation(Position position);
```

## How to add new Client API
By default, you have a Binance API client implemented buy, you may need to operate with another broker, so, in that case, you have to implement the IClientManager interface in libAPIManager.

```C#
public interface IClientManager
{
    public Task<IEnumerable<Candle>> GetHistoricAsync( TimeIntervalE intervalE, DateTime from, DateTime to, string symbol );

    public Task<string> GetSymbolInfoAsync( string symbol );
}
```

For the moment, only have these two methods. 

## How to check your strategy
You have so many examples of in StrategyTest project, just add new test method and good luck!.