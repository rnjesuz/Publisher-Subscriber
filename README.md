# Goal
The goal of this project is to design and implement a simplified (and therefore far from complete) implementation of a reliable, distributed, message broker supporting the publish-subscribe paradigm.

### Paradigm
The publish-subscribe system we are aiming at involves 3 types of processes:
* Publishers
* Subscribers
* Message brokers

Publishers are processes that produce events on one or more topics. Multiple publishers may publish events on the same topic.
Subscribers register their interest in receiving events of a given set of topics.
Brokers organize themselves in an overlay network, to route events from the publishers to the interested subscribers.

### Events
Events have two fields:  
* Topic
* Content

The topic can be seen as representing a hierarchical namespace. An event with topic "/news/olympicgames/2020/pingpong" would contain information relevant to the pinpong competitions of the olympic games of 2020.

To start receiving events a subscriber needs to the perform a subscription to the intended topic (hierarchical namespace). For instance, a subscriber may subscribe to "/news/olympicgames/\*" to receive events for all topics under the “/news/olympicgames/” prefix.

### Network
Communication among publishers and subscribers is indirect, via the network of brokers. Both publishers and subscribers connect to one broker (typically, the “nearest” broker in terms of network latency) and send/receive events to/from that broker.  Brokers coordinate among each other to propagate events in the overlay network.

* *Broker-Broker*:</br>
As noted before, brokers are organized in a tree.  Therefore, each broker only interacts directly with its parent and its children in the tree (if any). The communication among brokers aims at:
   * Propagating information regarding subscriptions and unsubscriptions, such that event routing can be optimized (in particular, such that events are not propagated in tree branches where there are no subscribers for those events).
   * Propagate the events along the tree.
* *Publisher-Broker*:</br>
The publishers interact with a broker to publish events. A publisher sends the events it produces to the broker at its site without being aware of other brokers nor of the number or location of subscribers for that event.
* *Subscriber-Broker*:</br>
Subscribers interact with the broker at their site.  Subscribers forward to their broker the subscription and unsubscription requests. Subscribers are not aware of the number and location of publishers  for the topics they subscribe. Also, subscribers export a callback that can be invoked by the broker to deliver events to the subscriber. There may be an arbitrary delay between the action of subscribing a topic and starting to receive messages on that topic. Once messages on a topic start being delivered to a subscriber, no message on that topic may be lost or delivered out of order.


# Configuration
There is a configuration file that describes the entire network: how many sites exist and which processes belong to each site. Also, for the purpose of building the overlay of brokers, we assume that sites are organized in a tree.

The first part of the configuration file enumerates the existing sites and specifies how the tree of sites is organized.</br>
> **Site** *sitename* **Parent** *sitename*|*none*

The rest of the configuration file specifies the logical names of the processes, their role, and the site they belong to. The final URL parameter designates the URL where a process is providing it’s services.</br>
> **Process** *processname* **Is** *publisher*|*subscriber*|*broker* **On** *sitename* **URL** *process-url*

The system implements two different policies for event routing:
* *Flooding*:</br>
In this approach events are broadcasted across the tree.
* *Filtering*:</br>
In this approach, events are only forwarded along paths leading to interested subscribers. To this end, brokers maintain information regarding which events should be forwarded to their neighbors.

The event routing policy to be used by the system is defined in the configuration file. If the line is missing, the system should default to flooding.</br>
> **RoutingPolicy** *flooding*|*filtering*

The system provides three types of ordering guarantees for the notification  of events, namely Total-order, FIFO-order and No-order.
* *Total order*:</br>
All events published with total order guarantees are delivered in the same order at all matching subscribers. More formally, if  two subscribers *s1*, *s2*  deliver events *e1*, *e2*, *s1* and *s2* deliver *e1* and *e2* in the same order. This ordering property is established on all events published with total order guarantee, independently of the identity of the producer and of the topic of the event.
* *FIFO order*:</br>
All events published with FIFO order guarantee by a publiser *p* are delivered in the same order according to which *p* published them.
* *No ordering*:</br>
As the name suggests, no guarantee is provided on the order of notification of events.

The event ordering guarantee is defined in the configuration file provided to all nodes. If the line is missing, the system defaults to FIFO.</br>
> **Ordering** *NO*|*FIFO*|*TOTAL*

Example:
```
RoutingPolicy filtering
Ordering TOTAL
Site site0 Parent none
Site site1 Parent site0
Site site2 Parent site0
Process broker0 Is broker On site0 URL tcp://localhost:3333/broker
Process publisher0 Is publisher On site0 URL tcp://localhost:3334/pub
Process subscriber0 Is subscriber On site0 URL tcp://localhost:3335/sub
Process subscriber1 Is subscriber On site0 URL tcp://localhost:3336/sub
```


# PuppetMaster
The PuppetMaster can send the following commands to the other processes:
* **Subscriber** *processname* **Subscribe** *topicname*</br>
This command is used to force asubscriber to subscribe to the given topic.
* **Subscriber** *processname* **Unsubscribe** *topicname*</br>
This command is used to force a subscriber to unsubscribe to the given topic.
* **Publisher** *processname* **Publish** *numberofevents* **Ontopic** *topicname* **Interval** *x_ms*</br>
This command is used to force a publisher to produce a sequence of *numberofevents* on a given topic. The publisher should sleep *x* milliseconds between two consecutive events. The content of these events should be a string that includes the name of the publisher and a sequence number.
* **Status**</br>
This command makes all nodes in the system print their current status. The status command should present brief information about the state of the system (who is present, which nodes are presumed failed, which subscriptions are active, etc...).
* **Crash** *processname*</br>
This command is used to force a process to crash (can be sent to publishers, subscribers or brokers).
* **Freeze** *processname*</br>
This command is used to simulate a delay in the process (can be sent to publishers, subscribers or brokers). After receiving a freeze, the process continues receiving messages but stops processing them until the PuppetMaster “unfreezes” it.
* **Unfreeze** *processname*</br>
This command is used to return a process to normal operation.  Pending messages that were received while the process was frozen, should be processed when this command is received.

To automate testing, the PuppetMaster can also read a sequence of such commands from a script file. An additional command is accepted when executing a script file:
* **Wait** *x_ms*</br>
This  command  instructs  the  PuppetMaster to sleep  for *x* milliseconds before reading and executing the following command in the file.
Example:
Freeze broker0
Wait 100
Unfreeze broker0

### Logging
The PuppetMaster produces a time ordered log of all events it triggers or observes. Publishers, brokers and subscribers notify the PuppetMaster whenever they publish, forward or receive a message.

There are two levels of logging, *light* and *full*. The  logging  level  to  be used by the system is defined in the configuration file:</br>
> **LoggingLevel** *full*|*light*

If the line is missing, the system defaults to light.
* In *full* logging mode, all events are included in the log.
* In *light* logging mode, the forwarding of events by the brokers are not included in the log and therefore the brokers don't notify the PuppetMaster of those events.
