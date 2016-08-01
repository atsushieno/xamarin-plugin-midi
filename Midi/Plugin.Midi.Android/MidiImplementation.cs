using Plugin.Midi.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Media.Midi;
using Android.Runtime;

using MidiDeviceInfo = Android.Media.Midi.MidiDeviceInfo;
using MidiDeviceDetails = Plugin.Midi.Abstractions.MidiDeviceDetails;
using MidiPortType = Plugin.Midi.Abstractions.MidiPortType;
using System.Threading.Tasks;
using System.Threading;

[assembly: UsesPermission (Android.Manifest.Permission.BindMidiDeviceService)]

namespace Plugin.Midi
{
	/// <summary>
	/// Implementation for Feature
	/// </summary>
	public class MidiImplementation : IMidi
	{
		public MidiImplementation ()
		{
			manager = Application.Context.GetSystemService (Context.MidiService).JavaCast<MidiManager> ();
		}

		MidiManager manager;

		public event EventHandler<MidiDeviceConnectionEventArgs> DeviceAdded;
		public event EventHandler<MidiDeviceConnectionEventArgs> DeviceRemoved;

		public IEnumerable<MidiDeviceDetails> GetDevices ()
		{
			return manager.GetDevices ().Select (d => CreateDetails (d));
		}

		static MidiDeviceDetails CreateDetails (MidiDeviceInfo d)
		{
			return new MidiDeviceDetails (
			      d.Id.ToString (),
			      d.Properties.GetString (MidiDeviceInfo.PropertyName),
			      d.GetPorts ().Select (p => new MidiPortDetails (
				    p.Type == Android.Media.Midi.MidiPortType.Input ? MidiPortType.Input : MidiPortType.Output,
				    p.Name,
					p.PortNumber.ToString ())).ToList ());
		}

		public Task<IMidiDevice> OpenDevice (string id)
		{
			return Task.Run (() => {
				var listener = new OpenListener ();
				manager.OpenDevice (manager.GetDevices ().First (d => d.Id.ToString () == id), listener, null);
				listener.WaitHandle.Wait ();
				return (IMidiDevice)listener.Result;
			});
		}

		class OpenListener : Java.Lang.Object, MidiManager.IOnDeviceOpenedListener
		{
			internal ManualResetEventSlim WaitHandle = new ManualResetEventSlim (false);

			internal AndroidMidiDevice Result;

			public void OnDeviceOpened (MidiDevice device)
			{
				Result = new AndroidMidiDevice (device, CreateDetails (device.Info));
				WaitHandle.Set ();
			}
		}

		class AndroidMidiDevice : IMidiDevice
		{
			MidiDevice device;

			internal AndroidMidiDevice (MidiDevice device, MidiDeviceDetails details)
			{
				this.device = device;
				this.Details = details;
			}

			public MidiDeviceDetails Details { get; private set; }

			public void Dispose ()
			{
				device.Close ();
			}

			public Task<IMidiInputPort> OpenInput (string portId)
			{
				return Task.FromResult ((IMidiInputPort) new AndroidMidiInputPort (device.OpenOutputPort (int.Parse (portId)), Details.Ports.First (p => p.PortId == portId)));
			}

			public Task<IMidiOutputPort> OpenOutput (string portId)
			{
				return Task.FromResult ((IMidiOutputPort)new AndroidMidiOutputPort (device.OpenInputPort (int.Parse (portId)), Details.Ports.First (p => p.PortId == portId)));
			}

			abstract class AndroidMidiPort : IMidiPort
			{
				protected AndroidMidiPort (MidiPortDetails details)
				{
					this.Details = details;
				}

				public MidiPortDetails Details { get; private set; }

				public abstract void Close ();

				public abstract void Dispose ();
			}

			private class AndroidMidiInputPort : AndroidMidiPort, IMidiInputPort
			{
				private MidiOutputPort port;

				public AndroidMidiInputPort (MidiOutputPort midiOutputPort, MidiPortDetails details)
					: base (details)
				{
					this.port = midiOutputPort;
					port.Connect (new Receiver (this));
				}

				class Receiver : MidiReceiver
				{
					AndroidMidiInputPort port;

					public Receiver (AndroidMidiInputPort androidMidiInputPort)
					{
						this.port = androidMidiInputPort;
					}

					public override void OnSend (byte [] msg, int offset, int count, long timestamp)
					{
						port.OnSend (msg, offset, count, timestamp);
					}
				}

				void OnSend (byte [] msg, int offset, int count, long timestamp)
				{
					MessageReceived (this, new MidiMessageEventArgs (Enumerable.Repeat (new MidiMessage (new ArraySegment<byte> (msg, offset, count), timestamp), 1)));
				}

				public event EventHandler<MidiMessageEventArgs> MessageReceived;

				public override void Close ()
				{
					port.Close ();
				}

				public override void Dispose ()
				{
					Close ();
				}
			}

			private class AndroidMidiOutputPort : AndroidMidiPort, IMidiOutputPort
			{
				private MidiInputPort port;

				public AndroidMidiOutputPort (MidiInputPort midiInputPort, MidiPortDetails details)
					: base (details)
				{
					this.port = midiInputPort;
				}

				public override void Close ()
				{
					port.Close ();
				}

				public override void Dispose ()
				{
					Close ();
				}

				public void Send (byte [] msg, int offset, int length)
				{
					Send (msg, offset, length, 0);
				}

				public void Send (byte [] msg, int offset, int length, long timestamp)
				{
					port.Send (msg, offset, length, timestamp);
				}
			}
		}
	}
}