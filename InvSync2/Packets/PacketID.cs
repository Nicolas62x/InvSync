namespace InvSync.Packets;
enum PacketID
{
    InvRequest,
    InvUpdate,
    InvDelete,

    InvDoesNotExist = 254,
    InvalidRequest = 255
}