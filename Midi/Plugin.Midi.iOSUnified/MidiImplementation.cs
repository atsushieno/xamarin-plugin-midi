using Plugin.Midi.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreMidi;

using CMidi = CoreMidi.Midi;
using CMidiPort = CoreMidi.MidiPort;
using System.Linq;
using System.Runtime.InteropServices;

namespace Plugin.Midi
{
	/// <summary>
	/// Implementation for Midi
	/// </summary>
	public class MidiImplementation : IMidi
	{
		public event EventHandler<MidiDeviceConnectionEventArgs> DeviceAdded;
		public event EventHandler<MidiDeviceConnectionEventArgs> DeviceRemoved;

		MidiClient client = new MidiClient ("XamarinMidiPluginClient");

		public MidiClient MidiClient {
			get { return client; }
		}

		public IEnumerable<MidiDeviceDetails> GetDevices ()
		{
			return Enumerable.Range (0, (int)CMidi.DeviceCount).Select (i => CreateMidiDeviceDetails (CMidi.GetDevice (i)));
		}

		public Task<IMidiDevice> OpenDevice (string id)
		{
			var devices = GetDevices ().ToArray ();
			var device = devices.First (i => i.Id == id);
			return Task.FromResult ((IMidiDevice)new IOSMidiDevice (this, CMidi.GetDevice (Array.IndexOf (devices, device)), device));
		}

		MidiDeviceDetails CreateMidiDeviceDetails (MidiDevice midiDevice)
		{
			return new MidiDeviceDetails (midiDevice.Name, midiDevice.DisplayName,
						   Enumerable.Range (0, (int)midiDevice.EntityCount).SelectMany (i => CreatePortDetailsList (midiDevice.GetEntity (i))));
		}

		IEnumerable<MidiPortDetails> CreatePortDetailsList (MidiEntity midiEntity)
		{
			return Enumerable.Range (0, (int)midiEntity.Sources).Select (i => CreatePortDetails (midiEntity.GetSource (i), MidiPortType.Input))
					 .Concat (Enumerable.Range (0, (int)midiEntity.Destinations).Select (i => CreatePortDetails (midiEntity.GetDestination (i), MidiPortType.Output)));
		}

		MidiPortDetails CreatePortDetails (MidiEndpoint endpoint, MidiPortType type)
		{
			return new IOSMidiPortDetails (endpoint, type, endpoint.DisplayName, endpoint.EndpointName);
		}
	}

	class IOSMidiPortDetails : MidiPortDetails
	{
		public IOSMidiPortDetails (MidiEndpoint endpoint, MidiPortType portType, string name, string portId)
			: base (portType, name, portId)
		{
			Endpoint = endpoint;
		}

		public MidiEndpoint Endpoint { get; private set; }
	}

	class IOSMidiDevice : IMidiDevice
	{
		internal IOSMidiDevice (MidiImplementation implementation, MidiDevice device, MidiDeviceDetails details)
		{
			this.implementation = implementation;
			this.device = device;
			this.Details = details;
		}

		MidiImplementation implementation;
		MidiDevice device;

		public MidiDeviceDetails Details { get; private set; }

		public void Dispose ()
		{
		}

		public Task<IMidiInputPort> OpenInput (string portId)
		{
			var port = (IOSMidiPortDetails)Details.Ports.First (p => p.PortId == portId);
			return Task.FromResult ((IMidiInputPort) new IOSMidiInputPort (implementation.MidiClient.CreateInputPort ("TheInputPortFor" + portId), port.Endpoint, port));
		}

		public Task<IMidiOutputPort> OpenOutput (string portId)
		{
			var port = (IOSMidiPortDetails)Details.Ports.First (p => p.PortId == portId);
			return Task.FromResult ((IMidiOutputPort)new IOSMidiOutputPort (implementation.MidiClient.CreateOutputPort ("TheOutputPortFor" + portId), port.Endpoint, port));
		}
	}

	abstract class IOSMidiPort : IMidiPort
	{
		protected IOSMidiPort (MidiPortDetails details)
		{
			this.Details = details;
		}

		public MidiPortDetails Details { get; private set; }

		public abstract void Close ();

		public abstract void Dispose ();
	}

	class IOSMidiInputPort : IOSMidiPort, IMidiInputPort
	{
		public IOSMidiInputPort (CMidiPort port, MidiEndpoint endpoint, MidiPortDetails details)
			: base (details)
		{
			this.port = port;
			this.endpoint = endpoint;
			endpoint.MessageReceived += (sender, e) => MessageReceived (
				sender, new MidiMessageEventArgs (e.Packets.Select (p => new MidiMessage (new ArraySegment<byte> (GetBytes (p)), p.TimeStamp))));
			port.ConnectSource (endpoint);
		}

		CMidiPort port;
		MidiEndpoint endpoint;

		static byte [] GetBytes (MidiPacket packet)
		{
			var bytes = new byte [packet.Length];
			Marshal.Copy (packet.Bytes, bytes, 0, packet.Length);
			return bytes;
		}

		public event EventHandler<MidiMessageEventArgs> MessageReceived;

		public override void Close ()
		{
			port.Disconnect (endpoint);
		}

		public override void Dispose ()
		{
			Close ();
		}
	}

	class IOSMidiOutputPort : IOSMidiPort, IMidiOutputPort
	{
		public IOSMidiOutputPort (CMidiPort port, MidiEndpoint endpoint, MidiPortDetails details)
			: base (details)
		{
			this.port = port;
			this.endpoint = endpoint;
		}

		CMidiPort port;
		MidiEndpoint endpoint;

		public override void Close ()
		{
			port.Disconnect (endpoint);
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
			port.Send (endpoint, new MidiPacket [] { new MidiPacket (timestamp, msg, offset, length) });
		}
	}
}
