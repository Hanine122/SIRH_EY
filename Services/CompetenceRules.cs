namespace SIRH.EY.Services
{
    public static class CompetenceRules
    {
        public static int GetSeuilRequis(string grade)
        {
            return grade switch
            {
                "Junior" => 1,
                "Senior" => 2,
                "Manager" => 4,
                _ => 1
            };
        }

        public static int GetNiveauCibleParGrade(string grade)
        {
            return grade switch
            {
                "Junior" => 3,
                "Senior" => 4,
                "Manager" => 5,
                _ => 3
            };
        }
    }
}