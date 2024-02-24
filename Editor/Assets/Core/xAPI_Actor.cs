namespace xAPI.Registry
{
    /// <summary>
    /// Version: 1.0.0
    /// </summary>
    public struct xAPI_Actor
    {
        public readonly string Name;
        public readonly string Email;

        public xAPI_Actor(string name, string email)
        {
            Name = name;
            Email = email;
        }
    }
}