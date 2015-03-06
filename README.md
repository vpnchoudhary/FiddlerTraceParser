# FiddlerTraceParser
Fiddler Trace Parser
This is the first version of this tool which does following:
 1) Take 3 arguments: baseurl, second url and fiddler trace file location
 2) extract the .saz file at same location
 3) create a csv file containing following information of all session started with baseurl:
 a) Network Latency accessing base url
 b) Navigation start time of second url relative to base url.
 c) elapsed time of second url.

The current implementation is very specific to my current requirement, but this code can become starting point for our specific trace parsing requirement.
