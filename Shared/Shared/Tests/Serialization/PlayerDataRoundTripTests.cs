using NUnit.Framework;
using LiteNetLib.Utils;
using Shared.Network.Packets.Game.Player;
using Game.Core.Common.Enums;
using Game.Core.Common.ValueObjetcs;

namespace Shared.Tests.Serialization;

[TestFixture]
public class PlayerDataRoundTripTests
{
    [Test]
    public void PlayerData_SerializeDeserialize_RoundTrip_Ok()
    {
        var original = new PlayerData
        {
            NetId = 42,
            Name = "Knight",
            Vocation = VocationEnum.None,
            Gender = GenderEnum.None,
            Direction = DirectionEnum.East,
            Speed = 2.5f,
            GridPosition = new MapPosition(10, 20),
            Description = "Test"
        };
        var writer = new NetDataWriter();
        original.Serialize(writer);

        var reader = new NetDataReader(writer.CopyData());
        var copy = new PlayerData();
        copy.Deserialize(reader);

        Assert.AreEqual(original.NetId, copy.NetId);
        Assert.AreEqual(original.Name, copy.Name);
        Assert.AreEqual(original.Vocation, copy.Vocation);
        Assert.AreEqual(original.Gender, copy.Gender);
        Assert.AreEqual(original.Direction, copy.Direction);
        Assert.AreEqual(original.Speed, copy.Speed);
        Assert.AreEqual(original.GridPosition, copy.GridPosition);
        Assert.AreEqual(original.Description, copy.Description);
    }
}
