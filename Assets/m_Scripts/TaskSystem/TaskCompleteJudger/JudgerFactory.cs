
public static class JudgerFactory
{
    public enum JudgerType
    {
        And,Or
    }
    
    public static ITaskCompleteJudger CreateJudger(JudgerType judgerType)
    {
        switch (judgerType)
        {
            case JudgerType.And:
                return new AndTaskCompleteJudger();
            case JudgerType.Or:
                return new OrTaskCompleteJudger();
            default:
                return null;
        }
    }
}
