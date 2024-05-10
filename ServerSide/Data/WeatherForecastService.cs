namespace ServerSide.Data
{
	public class WeatherForecastService
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		public Task<WeatherForecast[]> GetForecastAsync(DateTime startDate)
		{
			var mre = new ManualResetEvent(false);
			new Thread(() =>
			{
				Thread.Sleep(1000);
				mre.Set();
			}).Start();
			mre.WaitOne();
			Console.WriteLine("GetForecastAsync() being called");
			return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
			{
				Date = startDate.AddDays(index),
				TemperatureC = Random.Shared.Next(-20, 55),
				Summary = Summaries[Random.Shared.Next(Summaries.Length)]
			}).ToArray());
		}
	}
}
