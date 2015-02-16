Tunnelity
=========

Tunnelity is a C# socket server which allows socket client to communicate with your Unity game instance, and get different type of properties back.


=========

<h2> Add Tunnelity to Unity project from Editor </h2>

(1) Create a new folder <code> Tunnelity </code> under <code> #{YourUnityProject}/Assets/Plugins/ </code>, Download <code>Tunnelity.prefab</code> and <code> TunnelityServer.cs </code> to this new folder

(2) Open the first scene of the Unity project. (Find which is the first scene by going to File->Build Settings)

(3) Drag <code> Tunnelity.prefab </code> to the scene

(4) Click <code> Tunnelity.prefab </code> and check its script component, add the link to <code> TunnelityServer.cs </code>

=========


<h2> If "NGUI" is not in Assets/Plugins/ : </h2>

(1) Go to UnityProject/Assets/Plugins

(2) Download the NGUI files by doing: git clone git@las-ghub01-lnx.corp.kabam.com:unity-qa/NGUI.git

=========

<h2> If "Fuse" in not in Assets/Plugins/ :</h2>

(1) Go to UnityProject/Assets/Plugins

(2) Download the JsonFx files by doing: git clone git@las-ghub01-lnx.corp.kabam.com:unity-qa/JsonFx.git

(3) Update <code> Tunnelity.cs </code>, replace every <code> EB.JSON </code> with <code> JsonFx.JSON </code>

=========

<h2> Test whether it works:</h2>

(1) Launch the game in Unity Editor

(2) Run this ruby script:

 	require 'socket'
    require 'json'
	@host = '127.0.0.1'
	@port = 9921
	@s = TCPSocket.open(@host, @port)
    p "###############"
	p "socket server: #{@s}"
	p "socket initial get requrest: " + @s.gets
	p "###############"

	def send_josn_request_to_socket(json_request)
	  @s.puts(json_request)
	  @s.flush
	  json_response = @s.gets.chomp
	  JSON.parse(json_response)
	end

	json_request_for_get_screen = {"command" => "get_screen"}.to_json
	p send_josn_request_to_socket(json_request_for_get_screen)

	@s.close
	
(3) If everything works fine the response will be a valid JSON which contains the screen size	
 
=========










