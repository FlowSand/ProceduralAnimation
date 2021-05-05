//DEL
//功能：通用型BodyPart

public class GenericBodyPart : BodyPart
{
    public BodyChunk connection;

    public GenericBodyPart(GraphicsModule ow, float rd, float sfFric, float aFric, BodyChunk con)
      : base(ow)
    {
        rad = rd;
        connection = con;
        surfaceFric = sfFric;
        airFric = aFric;
        Reset(con.pos);
    }

    public override void Update()
    {
        //计算位置、速度
        lastPos = pos;
        pos += vel;
        vel *= airFric;
        //推离地形
        base.PushOutOfTerrain(owner.owner.room);
    }
}
