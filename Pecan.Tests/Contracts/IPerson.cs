namespace WPecanTests.Contracts
{
    public interface IPerson : ISocialMedia
    {
        string FirstName { set; get; }

        string PersonDescription { set; get; }

        string EmailAddress { set; get; }

        string LastName { set; get; }
    }
}