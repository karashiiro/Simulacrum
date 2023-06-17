from diagrams import Edge


class ServerBoundEdge(Edge):
    """Represents a request from the client to the server."""

    def __init__(self):
        super().__init__(color="blue")


class ClientBoundEdge(Edge):
    """Represents a response from the server to the client."""

    def __init__(self):
        super().__init__(color="red")


class BidirectionalEdge(Edge):
    """Represents a bidirectional stream between the client and the server."""

    def __init__(self):
        super().__init__(color="purple")
