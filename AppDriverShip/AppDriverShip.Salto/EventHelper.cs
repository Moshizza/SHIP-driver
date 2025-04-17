using System;
using System.Threading.Tasks;

namespace AppDriverShip.Salto;

public static class EventHelper
{
	public static async void InvokeAsync<T>(Action<T> action, T o)
	{
		if (action == null)
		{
			return;
		}
		try
		{
			await Task.Run(delegate
			{
				try
				{
					action?.Invoke(o);
				}
				catch (Exception ex3)
				{
					Console.Out.WriteLine(ex3.Message);
				}
			});
		}
		catch (TaskCanceledException)
		{
		}
		catch (Exception ex2)
		{
			Console.Out.WriteLine(ex2.Message);
		}
	}
}
