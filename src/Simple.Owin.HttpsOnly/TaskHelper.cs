namespace Simple.Owin
{
    using System.Threading.Tasks;

    class TaskHelper
    {
        private static Task _completed;

        public static Task Completed
        {
            get { return _completed ?? (_completed = MakeCompletedTask()); }
        }

        private static Task MakeCompletedTask()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetResult(0);
            return tcs.Task;
        }
    }
}