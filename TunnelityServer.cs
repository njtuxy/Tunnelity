using UnityEngine;
using System.Collections;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class TunnelityServer : MonoBehaviour
{
	public string ip = "127.0.0.1";
	public int port = 9921;
	public bool excludeSelfFromScene = true;
	
	private TcpListener listener = null;
	private List<TunnelityRequest> requests;
	
	public static TunnelityServer instance = null;
	
	void Awake()
	{
		//Only starts socket on debug builds.
		if (!Debug.isDebugBuild || instance != null)
		{
			Destroy(gameObject);
			return;
		}
		instance = this;
		// prevent ourselves being removed from the scene when loading new levels
		DontDestroyOnLoad(gameObject);
		// start server
		requests = new List<TunnelityRequest>();
		StartCoroutine(Listen());
	}
	
	private IEnumerator Listen()
	{
		yield return new WaitForEndOfFrame();
		
//		Debug.LogWarning("Tunnelity: Listening on " + ip + ":" + port.ToString());
		Debug.Log("Tunnelity: Listening on " + ip + ":" + port.ToString());
		
		IPAddress localAddr = IPAddress.Parse(ip);
		listener = new TcpListener(localAddr, port);
		
		// start listening
		listener.Start();
		
		// wait for our first client
		listener.BeginAcceptTcpClient(OnAcceptTcpClientComplete, this);	
		StartCoroutine(MainLoop());
		Debug.Log("4");
	}
	
	private IEnumerator MainLoop()
	{
		List<TunnelityRequest> pendingRequests;
		
		while (true)
		{
			pendingRequests = null;
			lock(requests)
			{
				if (requests.Count > 0)
				{
					pendingRequests = new List<TunnelityRequest>(requests);
					requests.Clear();
				}
			}
			if (pendingRequests != null)
			{
				pendingRequests.ForEach(ProcessRequest);
				yield return new WaitForEndOfFrame();
			}
			else
			{
				yield return new WaitForSeconds(0.1f);
			}
		}
	}
	
	private void ProcessRequest(TunnelityRequest req)
	{
		Hashtable res = new Hashtable();
		try
		{
			// read data from client as a JSON dictionary		
			Hashtable jsonReq = (Hashtable)EB.JSON.Parse(req.Request);			
			string cmdName = GetJsonString(jsonReq, "command");
			
			if ((cmdName == null) || (cmdName.Length == 0))
			{
				Debug.LogError("Tunnelity: missing command in client's request (" + req.Request + ")");
				res["error"] = "MissingCommandError";
				req.OnProcessed(EB.JSON.Stringify(res));
				return;
			}
			
			TunnelityCommand cmd;
			
			switch (cmdName)
			{
			case "get_screen":
				cmd = new TunnelityGetScreenCommand();
				break;
				
			case "get_scene":
				cmd = new TunnelityGetSceneCommand();
				break;
				
			case "get_level":
				cmd = new TunnelityGetLevelCommand();
				break;
				
			case "load_level":
				cmd = new TunnelityLoadLevelCommand();
				break;
				
			case "get_game_object":		  
				cmd = new TunnelityGetGameObjectCommand();
				break;
				
			case "click_game_object":     
				cmd = new TunnelityClickGameObjectCommand();
				break;
			case "get_game_object_child":
				cmd = new TunnelityGetGameObjectChild();
				break;
			case "get_FPS":
				cmd = new TunnelityGetFPSValue();
				break;
			default:
				Debug.LogError("Tunnelity: unknown command in client's request (" + req.Request + ")");
				res["error"] = "UnknownCommandError";
				req.OnProcessed(EB.JSON.Stringify(res));
				return;
			}
			
			cmd.Process(jsonReq, res);
		}
		catch (Exception e)
		{
			Debug.LogError("Tunnelity: exception while processing client's request (" + e + ")");
			res["error"] = "ServerError";
		}
		// reply to client
		req.OnProcessed(EB.JSON.Stringify(res));
	}
	
	private static string GetJsonString(Hashtable data, string key)
	{
		if ((data == null) ||
		    (!data.Contains(key)) ||
		    (data[key].GetType() != typeof(string)))
		{
			return null;
		}
		return data[key] as string;
	}
	
	private static bool GetJsonInt(Hashtable data, string key, ref int value)
	{
		if ((data == null) ||
		    (!data.Contains(key)) ||
		    (data[key].GetType() != typeof(int)))
		{
			return false;
		}
		value = (int)data[key];
		return true;
	}
	
	void OnDestroy()
	{
		try
		{
			if (listener != null)
			{
				listener.Stop();
			}
		}
		catch (SocketException)
		{
		}
	}
	
	public void EnqueueRequest(TunnelityRequest req)
	{
		lock(requests)
		{
			requests.Add(req);
		}
	}
	
	static private void OnAcceptTcpClientComplete(IAsyncResult result) 
	{
		TunnelityServer server = result.AsyncState as TunnelityServer;
		TcpClient client = server.listener.EndAcceptTcpClient(result);	
		Debug.Log("client connected!");
		TunnelityClient cukieClient = new TunnelityClient(client, server);
		Debug.Log("1");
		// ready for a new client
		server.listener.BeginAcceptTcpClient(OnAcceptTcpClientComplete, server);
		Debug.Log("2");
		// start  client		
		cukieClient.SendAck();
		Debug.Log("3");
	}
	
	public class TunnelityCommand
	{
		public delegate object GameObjectVisitor(GameObject obj, object context);
		
		public virtual void Process(Hashtable req, Hashtable res)
		{
			throw new System.Exception("Not implemented");
		}
		
		protected void Traverse(GameObject obj, GameObjectVisitor visitor, object context)
		{
			context = visitor(obj, context);
			foreach (Transform child in obj.transform)
			{
				Traverse(child.gameObject, visitor, context);
			}
		}
	}
	
	public class TunnelityGetSceneCommand : TunnelityCommand
	{
		public delegate void SerializerMethod(Component c, Hashtable h);
		
		public override void Process(Hashtable req, Hashtable res)
		{
			// get all objects on the root
			foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
			{
				//        if ((obj.transform.parent == null) &&
				//            // exclude this game object?
				//            ((!TunnelityServer.instance.excludeSelfFromScene) ||
				//             ((TunnelityServer.instance.excludeSelfFromScene) &&
				//              (obj != TunnelityServer.instance.gameObject))))
				//        {
				//          objs.Add(obj);
				//        }
				
				
				
				Debug.LogWarning("################### Tunnelity log ####################> object Name: " + obj.name);
			}
			
			//      // traverse all objects recursively
			//      List<Hashtable> rootObjects = new List<Hashtable>();
			//      res["gameObjects"] = rootObjects;
			//      foreach (GameObject obj in objs)
			//      {
			//		Debug.LogWarning("################### Tunnelity log ####################> object Name: " + obj.name);
			//        //Traverse(obj, SerializeGameObject, rootObjects);
			//				
			//		if (obj.name=="UI Root (2D)")
			//				{
			//					Debug.LogWarning("&&&&&&&&&&&&&&&&&&&&&&&&&&& Tunnelity log &&&&&&&&&&&&&&&&&&&&&&&&&&& found the UI Root 2d " + obj.name +" "+ obj.transform.childCount);
			//                }				
			//      }
		}
		
		private object SerializeGameObject(GameObject obj, object context)
		{
			Hashtable objHash = new Hashtable();
			objHash["instanceID"] = obj.GetInstanceID();
			objHash["name"] = obj.name;
			objHash["tag"] = obj.tag;
			objHash["active"] = obj.activeSelf;
			
			Hashtable layerHash = new Hashtable();
			layerHash["name"] = LayerMask.LayerToName(obj.layer);
			layerHash["value"] = obj.layer;
			objHash["layer"] = layerHash;
			
			objHash["children"] = new List<Hashtable>();
			objHash["components"] = SerializeComponents(obj);
			
			List<Hashtable> objs = context as List<Hashtable>;
			if (objs != null)
			{
				objs.Add(objHash);
			}
			
			return objHash["children"];
		}
		
		private List<Hashtable> SerializeComponents(GameObject obj)
		{
			List<Hashtable> components = new List<Hashtable>();
			Component[] comps = obj.GetComponents<Component>();
			
			foreach (Component comp in comps)
			{
				Hashtable compHash = new Hashtable();
				
				compHash["instanceID"] = comp.GetInstanceID();
				compHash["type"] = GetComponentTypeString(comp);
				
				Behaviour behaviour = null;
				// is this Component a Behaviour?
				if (typeof(Behaviour).IsAssignableFrom(comp.GetType()))
				{
					behaviour = comp as Behaviour;
					compHash["enabled"] = behaviour.enabled;
				}
				// serialize component
				SerializeComponent(comp, compHash);
				
				// add it to the list of components
				components.Add(compHash);
			}
			return components;
		}
		
		public static string GetComponentTypeString(Component comp, Type t = null)
		{
			if (t == null)
			{
				t = comp.GetType();
			}
			string compType = t.ToString();
			
			// I have seen this happen for a Camera's FlareLayer
			if (compType == "UnityEngine.Behaviour")
			{
				// extract type while aavoiding reflection
				int pos = comp.ToString().LastIndexOf("(UnityEngine.");
				if (pos >= 0)
				{
					string compType2 = comp.ToString().Substring(pos);
					if ((compType2.Length >= "(UnityEngine.x)".Length) &&
					    (compType2.EndsWith(")")))
					{
						// fix type
						compType = compType2.Substring(1, compType2.Length - 2);
					}
				}
			}
			
			if (compType.StartsWith("UnityEngine."))
			{
				compType = compType.Remove(0, "UnityEngine.".Length);
			}
			return compType;
		}
		
		public static void SerializeComponent(Component comp, Hashtable compHash)
		{
			Type compType = comp.GetType();
			Dictionary<Type, SerializerMethod> methods = new Dictionary<Type, SerializerMethod>()
			{
				{ typeof(Transform), SerializeTransform },
				{ typeof(Camera), SerializeCamera },
				{ typeof(GUIElement), SerializeGUIElement },
				{ typeof(GUITexture), SerializeGUITexture },
				{ typeof(GUIText), SerializeGUIText }
			};
			
			// serialize subclasses first
			foreach (KeyValuePair<Type, SerializerMethod> pair in methods)
			{
				if (compType.IsSubclassOf(pair.Key))
				{
					pair.Value(comp, compHash);
				}
			}
			// serialize main classes after
			foreach (KeyValuePair<Type, SerializerMethod> pair in methods)
			{
				if (compType == pair.Key)
				{
					pair.Value(comp, compHash);
				}
			}
		}
		
		public static void SerializeTransform(Component comp, Hashtable h)
		{
			Transform t = comp as Transform;
			h["position"] = SerializeVector3(t.position);
			h["rotation"] = SerializeQuaternion(t.rotation);
			h["localScale"] = SerializeVector3(t.localScale);
		}
		
		public static void SerializeCamera(Component comp, Hashtable h)
		{
			Camera c = comp as Camera;
			h["fieldOfView"] = c.fieldOfView;
			h["nearClipPlane"] = c.nearClipPlane;
			h["farClipPlane"] = c.farClipPlane;
			h["renderingPath"] = c.renderingPath.ToString();
			h["actualRenderingPath"] = c.actualRenderingPath.ToString();
			h["orthographicSize"] = c.orthographicSize;
			h["orthographic"] = c.orthographic;
			h["depth"] = c.depth;
			h["aspect"] = c.aspect;
			h["cullingMask"] = c.cullingMask;
			h["backgroundColor"] = SerializeColor(c.backgroundColor);
			h["rect"] = SerializeRect(c.rect);
			h["pixelRect"] = SerializeRect(c.pixelRect);
			// FIXME: render texture
			//h["targetTexture"] = SerializeTexture(c.targetTexture);
			h["pixelWidth"] = c.pixelWidth;
			h["pixelHeight"] = c.pixelHeight;
			h["cameraToWorldMatrix"] = SerializeMatrix4x4(c.cameraToWorldMatrix);
			h["worldToCameraMatrix"] = SerializeMatrix4x4(c.worldToCameraMatrix);
			h["projectionMatrix"] = SerializeMatrix4x4(c.projectionMatrix);
			h["velocity"] = SerializeVector3(c.velocity);
			h["clearFlags"] = c.clearFlags.ToString();
			h["layerCullDistances"] = c.layerCullDistances;
			h["depthTextureMode"] = c.depthTextureMode.ToString();
		}
		
		public static void SerializeGUIElement(Component comp, Hashtable h)
		{
			GUIElement e = comp as GUIElement;
			AppendHint<Hashtable>(h, "screen", SerializeRect(e.GetScreenRect()));
		}
		
		public static void SerializeGUITexture(Component comp, Hashtable h)
		{
			GUITexture t = comp as GUITexture;
			h["color"] = SerializeColor(t.color);
			h["pixelInset"] = SerializeRect(t.pixelInset);
			// FIXME: render texture
			//h["texture"] = SerializeTexture(t.texture);
		}
		
		public static void SerializeGUIText(Component comp, Hashtable h)
		{
			GUIText t = comp as GUIText;
			h["text"] = t.text;
			// FIXME: serialize Material
			//h["material"] = ...
			h["pixelOffset"] = SerializeVector2(t.pixelOffset);
			// FIXME: serialize Material
			//h["font"] = ...
			h["alignment"] = t.alignment.ToString();
			h["anchor"] = t.anchor.ToString();
			h["lineSpacing"] = t.lineSpacing;
			h["tabSize"] = t.tabSize;
			h["fontSize"] = t.fontSize;
			h["fontStyle"] = t.fontStyle.ToString();
			AppendHint<string>(h, "text", t.text);
		}
		
		public static Hashtable AppendHint<T>(Hashtable h, string name, T hint)
		{
			Hashtable hints = h[".hints"] as Hashtable;
			if (hints == null)
			{
				hints = new Hashtable();
				h[".hints"] = hints;
			}
			List<T> hintsList;
			if (hints.Contains(name))
			{
				hintsList = hints[name] as List<T>;
			}
			else
			{
				hintsList = new List<T>();
				hints[name] = hintsList;
			}
			hintsList.Add(hint);
			return hints;
		}
		
		public static Hashtable SerializeColor(Color c)
		{
			Hashtable h = new Hashtable();
			h["r"] = c.r;
			h["g"] = c.g;
			h["b"] = c.b;
			h["a"] = c.a;
			h["grayscale"] = c.grayscale;
			return h;
		}
		
		public static Hashtable SerializeRect(Rect r)
		{
			Hashtable h = new Hashtable();
			h["x"] = r.x;
			h["y"] = r.y;
			h["width"] = r.width;
			h["height"] = r.height;
			return h;
		}
		
		public static Hashtable SerializeVector3(Vector3 v)
		{
			Hashtable h = new Hashtable();
			h["x"] = v.x;
			h["y"] = v.y;
			h["z"] = v.z;
			return h;
		}
		
		public static Hashtable SerializeVector2(Vector2 v)
		{
			Hashtable h = new Hashtable();
			h["x"] = v.x;
			h["y"] = v.y;
			return h;
		}
		
		public static Hashtable SerializeQuaternion(Quaternion q)
		{
			Hashtable h = new Hashtable();
			h["x"] = q.x;
			h["y"] = q.y;
			h["z"] = q.z;
			h["w"] = q.w;
			return h;
		}
		
		public static float[] SerializeMatrix4x4(Matrix4x4 m)
		{
			float[] matrix = new float[16];
			for (int index = 0; index < matrix.Length; ++index)
			{
				matrix[index] = m[index];
			}
			return matrix;
		}
	}
	
	public class TunnelityGetScreenCommand : TunnelityCommand
	{
		public override void Process(Hashtable req, Hashtable res)
		{
			res["width"] = Screen.width;
			res["height"] = Screen.height;
		}
	}
	
	public class TunnelityGetLevelCommand : TunnelityCommand
	{
		public override void Process(Hashtable req, Hashtable res)
		{
			res["number"] = Application.loadedLevel;
			res["name"] = Application.loadedLevelName;
			res["count"] = Application.levelCount;
		}
	}
	
	public class TunnelityGetGameObjectCommand:TunnelityCommand
	{
		public override void Process(Hashtable req, Hashtable res)
		{				
			Vector2 point;
			string object_name = GetJsonString(req, "object_name");
			GameObject obj = GameObject.Find(object_name);
			if(obj == null){
				res["object_found"] = "false";	
			}
			
			else{
				res["object_found"] = "true";
				
				if(GetJsonString(req, "is_builiding") == "true"){					
					point = Camera.main.WorldToScreenPoint(obj.transform.position);
				}				
				else{
					point = UICamera.mainCamera.WorldToScreenPoint(obj.transform.position);
					UILabel[] labels = obj.GetComponentsInChildren<UILabel>();
					for(int i=0; i< labels.Length; i++){
						res["text"+i] = labels[i].text;			
					}
				}
				res["x"] = point.x;
				res["y"] = point.y;
			}
			
		}
	}
	
	public class TunnelityClickGameObjectCommand:TunnelityCommand
	{
		public override void Process(Hashtable req, Hashtable res)
		{       
			string object_name = GetJsonString(req, "object_name");
			string click_type = GetJsonString(req, "type");
			GameObject obj = GameObject.Find(object_name);
			
			if(click_type == null)
				click_type = "OnClick";
			
			if(obj == null){
				res["object_found"] = "false"; 
			}
			else{ 
				if(obj.collider != null){
					obj.collider.SendMessage(click_type, SendMessageOptions.DontRequireReceiver);
					res["clicked"] = "true";
				}
				else{
					res["error"] = "Object " + object_name +" has no collider!"; 
				}
			}
		}
		
	}

	public class TunnelityGetGameObjectChild:TunnelityCommand
	{
		public override void Process(Hashtable req, Hashtable res)
		{				
			Vector2 point;
			string object_name = GetJsonString(req, "object_name");
			GameObject obj = GameObject.Find(object_name);
			if(obj == null){
				res["object_found"] = "false";	
			}
			
			else{
				res["object_found"] = "true";

				int child_index = Convert.ToInt32(GetJsonString(req, "child_index"));

				Transform child = obj.transform.GetChild(child_index);

//				if(GetJsonString(req, "i") == "true"){					
//					point = Camera.main.WorldToScreenPoint(obj.transform.position);
//				}				
//				else{
//					point = UICamera.mainCamera.WorldToScreenPoint(obj.transform.position);
//					UILabel[] labels = obj.GetComponentsInChildren<UILabel>();
//					for(int i=0; i< labels.Length; i++){
//						res["text"+i] = labels[i].text;			
//					}
//				}

				point = UICamera.mainCamera.WorldToScreenPoint(child.position);

				res["x"] = point.x;
				res["y"] = point.y;
				res["name"]  = child.name;
			}
			
		}
	}


	//Method to get FPS value back, need to work with FPSMonitor.cs
	public class TunnelityGetFPSValue:TunnelityCommand
	{
		public override void Process(Hashtable req, Hashtable res)
		{       

			res["FPS"] = (TunnelityFPSMonitor.FPSValueSum / TunnelityFPSMonitor.FPSCounter).ToString();


		}
		
	}

	
	
	public class TunnelityLoadLevelCommand : TunnelityCommand
	{
		public override void Process(Hashtable req, Hashtable res)
		{
			int levelNumber = -1;
			string levelName = GetJsonString(req, "name");
			string methodName = GetJsonString(req, "method");
			bool hasLevelNumber = GetJsonInt(req, "number", ref levelNumber);
			
			if ((hasLevelNumber) && (levelName != null))
			{
				Debug.LogError("Tunnelity: cannot specify both level name and number");
				res["error"] = "BothLevelNameOrNumberError";
				return;
			}
			
			if (((levelNumber < 0) || (levelNumber >= Application.levelCount)) &&
			    ((levelName == null) || (levelName.Length == 0)))
			{
				Debug.LogError("Tunnelity: missing level name/number");
				res["error"] = "MissingLevelNameOrNumberError";
				return;
			}
			
			if (methodName == null)
			{
				methodName = "sync";
			}
			
			switch (methodName.ToLower())
			{
			case "sync":
				try
				{
					if (hasLevelNumber)
					{
						Application.LoadLevel(levelNumber);
					}
					else
					{
						Application.LoadLevel(levelName);
					}
				}
				catch (Exception)
				{
					throw new System.Exception("Load level failed");
				}
				break;
				
			case "async":
				try
				{
					if (hasLevelNumber)
					{
						Application.LoadLevelAsync(levelNumber);
					}
					else
					{
						Application.LoadLevelAsync(levelName);
					}
				}
				catch (Exception)
				{
					throw new System.Exception("Load level failed");
				}
				break;
				
			case "additive":
				try
				{
					if (hasLevelNumber)
					{
						Application.LoadLevelAdditive(levelNumber);
					}
					else
					{
						Application.LoadLevelAdditive(levelName);
					}
				}
				catch (Exception)
				{
					throw new System.Exception("Load level failed");
				}
				break;
				
			case "additiveasync":
				try
				{
					if (hasLevelNumber)
					{
						Application.LoadLevelAdditiveAsync(levelNumber);
					}
					else
					{
						Application.LoadLevelAdditiveAsync(levelName);
					}
				}
				catch (Exception)
				{
					throw new System.Exception("Load level failed");
				}
				break;
				
			default:
				Debug.LogError("Tunnelity: unknown load level method in client's request");
				res["error"] = "UnknownLoadLevelMethodError";
				return;
			}
		}
	}
	
	public class TunnelityRequest
	{
		private string request;
		private ManualResetEvent signal;
		private string response;
		
		public TunnelityRequest(string request, ManualResetEvent signal)
		{
			this.request = request;
			this.signal = signal;
			response = "";
		}
		
		public string Request
		{
			get
			{
				return request;
			}
		}
		
		public string Response
		{
			get
			{
				return response;
			}
		}
		
		public void OnProcessed(string response)
		{
			this.response = response;
			// fire signal
			signal.Set();
		}
	}
	
	public class TunnelityClient
	{
		private TcpClient client;
		private NetworkStream stream;
		private byte[] buffer;
		private int bufferUsedCount;
		private string bufferedString;
		private TunnelityServer server;
		public ManualResetEvent signal;
		
		private static readonly int BufferIncrease = 1024;
		private static readonly Encoding enc = Encoding.UTF8;
		
		public TunnelityClient(TcpClient client, TunnelityServer server)
		{
			this.client = client;
			this.server = server;
			stream = client.GetStream();
			buffer = new byte[BufferIncrease];
			bufferUsedCount = 0;
			bufferedString = "";
			signal = new ManualResetEvent(false);
		}
		
		public void SendAck()
		{
			Hashtable ack = new Hashtable();
			byte[] ackBytes = enc.GetBytes(EB.JSON.Stringify(ack) + "\n");
			stream.BeginWrite(ackBytes, 0, ackBytes.Length, OnSendAckComplete, this); 
		}
		
		static private void OnSendAckComplete(IAsyncResult result)
		{
			TunnelityClient cukieClient = result.AsyncState as TunnelityClient;
			cukieClient.stream.EndWrite(result);
			cukieClient.BeginRead();
		}
		
		private void BeginRead()
		{
			// resize required?
			if (bufferUsedCount >= buffer.Length)
			{
				byte[] newBuffer = new byte[bufferUsedCount + BufferIncrease];
				Array.Copy(buffer, newBuffer, buffer.Length);
				buffer = newBuffer;
			}
			stream.BeginRead(buffer, bufferUsedCount, buffer.Length - bufferUsedCount, OnReadComplete, this);
		}
		
		public void Close()
		{
			Debug.LogWarning("Tunnelity: client disconnected");
			try
			{
				stream.Close();
				client.Close();
			}
			catch (Exception)
			{
			}
		}
		
		static private void OnReadComplete(IAsyncResult result)
		{
			TunnelityClient cukieClient = result.AsyncState as TunnelityClient;
			int byteCount = cukieClient.stream.EndRead(result);
			string line;
			
			// nothing received?
			if (byteCount <= 0)
			{
				cukieClient.Close();
				return;
			}
			
			// append read data to internal buffer
			cukieClient.bufferedString += enc.GetString(cukieClient.buffer, cukieClient.bufferUsedCount, byteCount);
			cukieClient.bufferUsedCount += byteCount;
			
			// can we extract a line from this buffer?
			while ((line = cukieClient.ExtractLine(cukieClient.bufferedString)) != null)
			{
				int lineByteCount = enc.GetByteCount(line);
				cukieClient.bufferedString = cukieClient.bufferedString.Remove(0, lineByteCount);
				cukieClient.bufferUsedCount -= lineByteCount;
				
				// process received line
				string response = cukieClient.ProcessLineRequest(line);
				if (response.Length > 0)
				{
					// respond to client
					byte[] responseBytes = enc.GetBytes(response + "\n");
					cukieClient.stream.BeginWrite(responseBytes, 0, responseBytes.Length, null, null); 
				}
				else
				{
					cukieClient.Close();
					return;
				}
			}
			
			// next read
			cukieClient.BeginRead();
		}
		
		private string ExtractLine(string data)
		{
			int newLine = data.IndexOf("\n");
			
			if (newLine < 0)
			{
				return null;
			}
			return data.Substring(0, newLine + 1);
		}
		
		private string ProcessLineRequest(string line)
		{
			string request = line.TrimEnd('\r', '\n');
			TunnelityRequest req = new TunnelityRequest(request, signal);
			
			// reset signal
			signal.Reset();
			// add request to que
			server.EnqueueRequest(req);
			// wait for completion
			signal.WaitOne();
			return req.Response;
		}
	}
}
