namespace InExaxtObjectMapping.Tests.SampleTypes
{
    public class RandomType
    {
        public int RandomTypeUnmatchedProperty1 { get; private set; }

        public string RandomTypeUnmatchedProperty2 { get; private set; }

        public int MatchedProperty1 { get; private set; }

        public string MatchedProperty2 { get; private set; }

        public RandomType(int a, string b, int c, string d)
        {
            RandomTypeUnmatchedProperty1 = a;
            RandomTypeUnmatchedProperty2 = b;
            MatchedProperty1 = c;
            MatchedProperty2 = d;
        }
    }
}