public interface IScoreRule<TInput>
{
    int CalculateScore(TInput input);
}