# Updating...
TO BE UPDATING...
# Summary
Odenwald is a tool which switch the data from points to destination, and supports following plugins:
- OPC UA
- OPC DA (opc classic)
- InfluxDB
- Kafka
- Console

# Disclaimer
The following binaries belong to the OPC Foundation (https://opcfoundation.org/):

- OPC.Ua.Client.dll
- OPC.Ua.Core.dll
- OPC.Ua.Configuration.dll
- OpcComRcw.dll
- OpcNetApi.Com.dll
- OpcNetApi.dll

You should become a registered user in order to use.

Beside some popular packages as thousands opensouces (such as: log4net, newtonsoft.json, etc.), Odenwald use:
- InfluxDB.Net-Main
- kafka-net

# Reference
In order to test with OPC UA you should register and get the sample OPC UA server as (https://github.com/OPCFoundation/UA-.NET/tree/master/SampleApplications/Samples/Server)
In case of OPC DA testing, you are able to use OPC Classic Demo server from Softing (of course before using it, you should register as a user, althought that is a free of charge software),
http://industrial.softing.com/en/products/software/opc-development-toolkits/opc-classic-development-toolkits/opc-classic-da-ae-xml-da-client-and-server-toolkit-for-windows.html

Besides, Odenwald refers from these systems:
- collectd (https://github.com/collectd/collectd)
- CollectdWin (https://github.com/bloomberg/collectdwin)

# TODO
- Installation package use wix toolset.
- A small web app to monitor the service.
- MongoDB plugin.
- Update document. 
- Recode MetricData


