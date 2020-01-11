# Goal
The goal of this project is to design and implement a simplified (and therefore far from complete) implementation of a reliable, distributed, message broker supporting the publish-subscribe paradigm.

### Paradigm
The publish-subscribe system we are aiming at involves 3 types of processes: 
* Publishers
* Subscribers
* Message  brokers

Publishers are processes that produce events on one or more topics. Multiple publishers may publish events on the same topic.

Subscribers register their interest in receiving events of a given set of topics.

Brokers organize themselves in an overlay network, to route events from the publishers to the interested subscribers. 

### Network
Communication among publishers and subscribers is indirect, via the network of brokers. Both publishers and subscribers connect to one broker (typically, the “nearest” broker in terms of network latency) and send/receive events to/from that broker.  Brokers coordinate among each other to propagate events in the overlay network.
