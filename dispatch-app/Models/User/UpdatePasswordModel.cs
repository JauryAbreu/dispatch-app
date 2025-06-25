namespace dispatch_app.Models.User
{
    public class UpdatePasswordModel
    {
        public string LastPassword { get; set; }
        public string NewPassword { get; set; }
        public string RepeatPassword { get; set; }
    }
}
