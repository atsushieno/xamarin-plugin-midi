using Plugin.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace MidiSample
{
	public class App : Application
	{
		public App ()
		{
			var btn = new Button {
				Text = "Welcome to Xamarin Forms!",
			};
			btn.Clicked += Btn_Clicked;

			// The root page of your application
			MainPage = new ContentPage {
				Content = new StackLayout {
					VerticalOptions = LayoutOptions.Center,
					Children = {btn
		    }
				}
			};
		}

		private void Btn_Clicked (object sender, EventArgs e)
		{
			var midi = CrossMidi.Current;
			var info = midi.GetDevices ().First ();
			var device = midi.OpenDevice (info.Id).Result;
			var outport = device.OpenOutput (info.Ports.First (p => p.PortType == Plugin.Midi.Abstractions.MidiPortType.Output).PortId).Result;
			outport.Send (new byte [] { 0xC0, 0 }, 0, 2);
			outport.Send (new byte [] { 0x90, 0x40, 0x70 }, 0, 3);
			((Button)sender).Text = "noteon sent.";
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
