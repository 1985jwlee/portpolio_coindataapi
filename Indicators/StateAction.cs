public enum StateAction
{
    Buy, Sell, Neutral, StrongBuy, StrongSell
}

public static class StateActionToString
{
    public static string StateActionStr(this StateAction action)
    {
        switch (action)
        {
            case StateAction.Buy: return "buy";
            case StateAction.Sell: return "sell";
            case StateAction.StrongBuy: return "strong buy";
            case StateAction.StrongSell: return "strong sell";
        }

        return "neutral";
    }
}