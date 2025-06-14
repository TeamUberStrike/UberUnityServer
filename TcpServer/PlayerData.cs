using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerData
{
    // Identity
    public int id;
    public string name = "not set";

    // Transform
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;

    float xr = 0.0f;
    float yr = 0.0f;
    float zr = 0.0f;

    float xc = 0.0f;
    float yc = 0.0f;
    float zc = 0.0f;

    //crouch
    public bool crouch = false;

    public bool destroy = false;
    public bool die = false;
    public bool isAlive = false;

    // stats
    public float kills = 0;
    public float deaths = 0;

    // Current weapon ID
    public bool weaponChanged = false;
    public int weapon = 1;

    // appereances
    public bool appereancesChanged = false;
    public int holo = -1;
    public int head = -1;
    public int face = -1;
    public int gloves = -1;
    public int upperbody = -1;
    public int lowerbody = -1;
    public int boots = -1;

    public int lastKiller = 0;

    // Weapon shoot
    public bool pendingPrimaryFire = false;

    public bool pendingFinalWord = false;

    public PlayerData(string name)
    {
        this.name = name;
    }
    public System.Numerics.Vector3 position
    {
        get { return new System.Numerics.Vector3(x, y, z); }
        set { x = value.X; y = value.Y; z = value.Z; }
    }

    public System.Numerics.Vector3 rotation
    {
        get { return new System.Numerics.Vector3(xr, yr, zr); }
        set { xr = value.X; yr = value.Y; zr = value.Z; }
    }

    public System.Numerics.Vector3 subRotation
    {
        get { return new System.Numerics.Vector3(xc, yc, zc); }
        set { xc = value.X; yc = value.Y; zc = value.Z; }
    }
}
