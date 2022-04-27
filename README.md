
# InvSync
A software meant to **sync multiple minecraft servers**, to achieve this, this software **act as a fast database** that can be used to **store files shared by multiples servers**.

# How to run

Just **run** the **InvSync** file on your server.

You can install the **.Net runtime** on your server **to use the small Releases** marked with "DotNetX".

# How to connect

To talk with this software you'll need to use a **TCP Socket** :
```csharp
using Socket s = new Socket(SocketType.Stream, ProtocolType.Tcp);

//You may want to replace IPAddress.IPv6Loopback with the actual IP of you InvSync
//And replace 7342 with the actual port of the InvSync
s.Connect(new IPEndPoint(IPAddress.IPv6Loopback, 7342));

//Not mendatory
s.NoDelay = true;
s.LingerState.Enabled = false;
```

> Small example on how you can connect in **C#** a TCP socket to the software
> **Note:**  NoDelay and LingerState might improve latency of requests.

#  Protocol

### Requests :

The protocol currently consist of 3 requests:

| Name           |Usage                          						    |
|----------------|----------------------------------------------|
|InvRequest      |Used to **Get** a file on the from the InvSync|
|InvUpdate       |Used to **Save** a file on the InvSync        |
|InvDelete       |Used to **Delete** a file on the InvSync      |

### Packet structure :

Every packets follow the same base structure:

| FieldSize      | Fields   | Notes
|----------------|----------|--------------------------------
|4 bytes         |Int32     | Full packet length excluding the size of this field
|1 byte          |Byte      | Packet ID
|x bytes         |N/A       | Content of the packet, this is determined by the ID
>**Note:**  Every fields are **little endian**, you might need to reverse them back to big endian if your platform use big endian values.


- #### InvRequest : (ID = 0)

	| FieldSize      | Fields   | Notes
	|----------------|----------|-------
	|1 byte          |Byte      | Size of the File Name
	|x bytes         |Text      | File Name (UTF8)


	***Response OK : (ID = 0)***

	| FieldSize      | Fields       | Notes
	|----------------|------------|-------
	|x bytes           | File dat    | Content of the file

	***Response File not found : (ID = 254)***

	| FieldSize      | Fields       | Notes
	|----------------|------------|-------
	|0 bytes          |nothing     | this is an empty packet

- #### InvUpdate : (ID = 1)

	| FieldSize      | Fields   | Notes
	|----------------|----------|-------
	|1 byte          |Byte      | Size of the File Name
	|x bytes         |Text      | File Name (UTF8)
	|x bytes         |File dat | Content of the file


	***Response OK : (ID = 1)***

	| FieldSize      | Fields       | Notes
	|----------------|------------|-------
	|0 bytes          |nothing     | this is an empty packet


- #### InvDelete : (ID = 2)

	| FieldSize      | Fields   | Notes
	|----------------|----------|-------
	|1 byte          |Byte      | Size of the File Name
	|x bytes         |Text      | File Name (UTF8)


	***Response OK : (ID = 2)***

	| FieldSize      | Fields       | Notes
	|----------------|------------|-------
	|0 bytes          |nothing     | this is an empty packet
	

**Response Other Error : (ID = 255)**

| FieldSize      | Fields       | Notes
|----------------|------------|-------
|0 bytes          |nothing     | this is an empty packet
>**Note:**  This error is not specific to a request and indicate that your are probably sending invalid packets to the InvSync
