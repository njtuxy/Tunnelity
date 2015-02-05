Tunnelity
=========

Tunnelity is a C# socket server which allows any socket client to access a running Unity game's properties. 


=========
<h2> How to set it up for your Unity game </h2>

(1) Copy <code> Tunnelity.prefab </code>  and  <code> TunnelityServer.cs </code>  to <code> UnityGameFolder/Assets/Plugins/ </code> 

(2) Open Unity editor and open the scene that will be loadded when the game starts.

(3) In Project view locate Tunnelity.prefab and drag it to the scene.

(4) Click <code> Tunnelity.prefab </code>  and add the link to <code> TunnelityServer.cs </code>.

=========
<h2> Install JsonFx to your Unity project if you don't have it </h2>

(1) Go to UnityProject/Assets/Plugins

(2) Download the JsonFx files by doing: git clone git@las-ghub01-lnx.corp.kabam.com:unity-qa/JsonFx.git

=========
<h2> Install NGUI to your Unity project if you don't have it </h2>

(1) Go to UnityProject/Assets/Plugins

(2) Download the NGUI files by doing: git clone git@las-ghub01-lnx.corp.kabam.com:unity-qa/NGUI.git



<h2> Give it a test </h2>

(1) Launch the game on you machine.

(2) Write a custome socket client which can send json request to <code> localhost:9921 </code> , here is a ruby example:

 
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
 
=========
<h2> More examples: </h2>











