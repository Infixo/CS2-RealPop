using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace RealPop;

public static class Debug
{
	public static bool Logging { get; set; } = true;

    public static void Log(string text)
    {
        if (Logging)
        {
            UnityEngine.Debug.Log(GetCallingMethod(2) + ": " + text);
        }
	}

	public static void Log(StringBuilder text)
	{
        if (Logging)
        {
            UnityEngine.Debug.Log(GetCallingMethod(2) + ": " + text.ToString());
        }
	}

	public static void Log(params object[] args)
	{
		StringBuilder text = new();
		foreach (object o in args) text.Append(o.ToString());
        if (Logging)
        {
            UnityEngine.Debug.Log(GetCallingMethod(2) + ": " + text.ToString());
        }
    }

    public static void LogError(string sText)
	{
		if (Logging) UnityEngine.Debug.Log(GetCallingMethod(2) + " ERROR: " + sText);
        // this function is a bit useless... it just adds an extra line to the log with a reference to .cpp file, always the same
        //if (Logging) UnityEngine.Debug.LogError(GetCallingMethod() + ": " + text);
    }
    public static void LogWarning(string sText)
    {
        //if (Logging) UnityEngine.Debug.LogWarning(GetCallingMethod() + ": " + text);
        if (Logging) UnityEngine.Debug.Log(GetCallingMethod(2) + " WARNING: " + sText);
    }

    public static void Log(Exception ex)
	{
		if (!Logging) { return; }
        Log("EXCEPTION");
        UnityEngine.Debug.LogException(ex);
        if (ex.InnerException != null)
            UnityEngine.Debug.LogException(ex.InnerException);
    }

    public static void Log(string sText, Exception ex)
	{
        if (!Logging) { return; }
        Log("EXCEPTION " + sText);
        UnityEngine.Debug.LogException(ex);
		if (ex.InnerException != null)
			UnityEngine.Debug.LogException(ex.InnerException);
	}

    /// <summary>
    /// Gets the method from the specified <paramref name="frame"/>.
    /// </summary>
    public static string GetCallingMethod(int frame)
    {
        StackTrace st = new StackTrace();
        MethodBase mb = st.GetFrame(frame).GetMethod(); // 0 - GetCallingMethod, 1 - Log, 2 - actual function calling a Log method
        return mb.DeclaringType + "." + mb.Name;
    }

    /// <summary>
    /// Gets the calling method from the game engine (if possible).
    /// </summary>
    public static string GetGameCallingMethod()
    {
        StackTrace st = new StackTrace();
        for (int i = 0; i < st.FrameCount; i++)
        {
            MethodBase mb = st.GetFrame(i).GetMethod();
            string ns = mb.DeclaringType.Namespace;
            if (ns == null || ns == "ICities" || ns.Substring(0, Math.Min(17, ns.Length)) == "ColossalFramework")
                return mb.DeclaringType + "." + mb.Name;
        }
        return "unknown";
    }

    /// <summary>
    /// Gets the calling stack from the game engine (if possible).
    /// </summary>
    public static string GetGameCallingStack(int numMethods = 4)
    {
        StringBuilder sb = new();
        int num = 0;
        StackTrace st = new StackTrace();
        for (int i = 0; i < st.FrameCount; i++)
        {
            MethodBase mb = st.GetFrame(i).GetMethod();
            string ns = mb.DeclaringType.Namespace;
            if (ns == null || ns == "ICities" || ns.Substring(0, Math.Min(17, ns.Length)) == "ColossalFramework")
            {
                if (num++ > 0) sb.Append(",");
                sb.Append(mb.DeclaringType + "." + mb.Name);
                if (num == numMethods) return sb.ToString();
            }
        }
        return num > 0 ? sb.ToString() : "unknown";
    }


    /*
        // TEST
        string stackIndent = "";
        for (int i = 0; i < st.FrameCount; i++)
        {
            StackFrame sf = st.GetFrame(i);
            MethodBase method = sf.GetMethod();
            UnityEngine.Debug.Log(stackIndent + $" Frame [{i}] Method: {method}");
            UnityEngine.Debug.Log(stackIndent + $" declType: {method.DeclaringType} name: {method.Name} type: {method.GetType()}");
            UnityEngine.Debug.Log(stackIndent + $" reflType: {method.ReflectedType} module: {method.Module}");
            UnityEngine.Debug.Log(stackIndent + $" membType: {method.MemberType}");
            stackIndent += " ";
            // MemberType - Method, etc.
            // Module - data-00000000825A26B0, not very informative
            // DeclaringType is the type that declares the method - i.e. it will always show where the method is "coded"
            // ReflectedType is the type object that was used to retrieve the method - i.e. it will show actual object used during an execution, could be a derived class
        }
    */
}


public static class Logging
{
    private static readonly string m_CsvFileName = "c:/log.csv"; // System.IO.Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "BigBrotherLog.csv");

    private static void LogToFile(string line)
    {
        try
        {
            using (var sw = new StreamWriter(m_CsvFileName, true, Encoding.UTF8)) // append mode, creates a new file if one doesn't exist
            {
                sw.WriteLine(line);
                sw.Close();
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Cannot write to {m_CsvFileName}", e);
        }
    }

    /// <summary>
    /// Formats arguments into CSV-string and adds to the log.
    /// </summary>
    /// <param name="identifier">Unique identifier of the record type..</param>
    /// <param name="args">Data items</param>
    public static void Add(string identifier, params object[] args)
    {
        // universal reference
        TimeSpan ts = DateTime.Now.TimeOfDay;
        long msecs = ts.Ticks / 10000;
        long ticks = ts.Ticks % 10000;

        // game reference
        uint frame = 0; //  Singleton<SimulationManager>.instance.m_currentFrameIndex;
        uint week = (frame >> 12);

        StringBuilder sb = new StringBuilder();
        sb.Append($"{msecs}.{ticks:D4}");
        sb.Append($",{frame:X}");
        sb.Append("," + week);
        sb.Append("," + identifier);
        foreach (object arg in args) sb.Append(","+arg.ToString());
        //if (ModSettings.Instance.CallingMethod) sb.Append("," + Debug.GetGameCallingStack());

        // output to .csv or output_log.txt
        //if (ModSettings.Instance.CSVFile)
            //LogToFile(sb.ToString());
        //else
            UnityEngine.Debug.Log(sb.ToString());
    }
    /*
    /// <summary>
    /// Helper to mark Outside Connections.
    /// </summary>
    /// <returns><typeparamref name="string"/>: NOCON, CONIN, CONOUT, CONBOTH</returns>
    public static string CheckOutsideConnection(ushort building)
    {
        if (building == 0) { return "NOCON"; }
        Building.Flags flags = Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].m_flags;
        if ((flags & Building.Flags.IncomingOutgoing) == Building.Flags.IncomingOutgoing) return "CONBOTH";
        if ((flags & Building.Flags.Incoming) != 0) return "CONIN";
        if ((flags & Building.Flags.Outgoing) != 0) return "CONOUT";
        return "NOCON";
    }

    /// <summary>
    /// Helper. Formats properly a <typeparamref name="TransferOffer"/> struct.
    /// </summary>
    public static string Format(TransferOffer offer)
    {
        return $"{offer.m_object.Type},{offer.m_object.Index},{CheckOutsideConnection(offer.Building)},{offer.Priority},{offer.Amount}";
    }

    /// <summary>
    /// Helper. Formats properly a <typeparamref name="Citizen"/> struct.
    /// </summary>
    public static string Format(ref Citizen data)
    {
        return $"{Citizen.GetAgeGroup(data.Age)}.{data.Age},{data.m_instance}," +
            $"{data.CurrentLocation},{data.m_homeBuilding},{data.m_workBuilding},{data.m_visitBuilding},{data.m_vehicle}," +
            $"{Logging.DecodeFlags(data.m_flags)}";
    }

    /// <summary>
    /// List of tracked materials / reasons.
    /// </summary>
    private static readonly TransferReason[] Reasons = {
        TransferReason.Sick, TransferReason.Sick2, TransferReason.SickMove, TransferReason.Dead, TransferReason.DeadMove,
        TransferReason.Single0, TransferReason.Single1, TransferReason.Single2, TransferReason.Single3,
        TransferReason.Single0B, TransferReason.Single1B,TransferReason.Single2B, TransferReason.Single3B,
        TransferReason.PartnerYoung, TransferReason.PartnerAdult,
        TransferReason.Worker0, TransferReason.Worker1, TransferReason.Worker2, TransferReason.Worker3,
        TransferReason.Student1, TransferReason.Student2,TransferReason.Student3,
        TransferReason.Family0, TransferReason.Family1, TransferReason.Family2, TransferReason.Family3,
        TransferReason.LeaveCity0, TransferReason.LeaveCity1, TransferReason.LeaveCity2,
        TransferReason.Garbage, TransferReason.Goods,
        TransferReason.Shopping, TransferReason.ShoppingB, TransferReason.ShoppingC, TransferReason.ShoppingD,
        TransferReason.ShoppingE, TransferReason.ShoppingF, TransferReason.ShoppingG, TransferReason.ShoppingH,
    };
    
    /// <summary>
    /// Checks if the specified material (aka transfer reason) is tracked by the mod.
    /// </summary>
    public static bool IsTracked(TransferReason material)
    {
        return Array.IndexOf(Reasons, material) >= 0;
    }

    public static string DecodeFlags(Citizen.Flags flags)
    {
        if (flags == Citizen.Flags.None) return "|0|";
        StringBuilder txt = new("|");
        //if ((flags & Citizen.Flags.Created) != 0) txt.Append($"{Citizen.Flags.Created}|");
        if ((flags & Citizen.Flags.Tourist) != 0) txt.Append($"{Citizen.Flags.Tourist}|");
        if ((flags & Citizen.Flags.Sick) != 0) txt.Append($"{Citizen.Flags.Sick}|");
        if ((flags & Citizen.Flags.Dead) != 0) txt.Append($"{Citizen.Flags.Dead}|");
        if ((flags & Citizen.Flags.Student) != 0) txt.Append($"{Citizen.Flags.Student}|");
        if ((flags & Citizen.Flags.MovingIn) != 0) txt.Append($"{Citizen.Flags.MovingIn}|");
        if ((flags & Citizen.Flags.DummyTraffic) != 0) txt.Append($"{Citizen.Flags.DummyTraffic}|");
        if ((flags & Citizen.Flags.Criminal) != 0) txt.Append($"{Citizen.Flags.Criminal}|");
        if ((flags & Citizen.Flags.Arrested) != 0) txt.Append($"{Citizen.Flags.Arrested}|");
        //if ((flags & Citizen.Flags.Evacuating) != 0) txt.Append($"{Citizen.Flags.Evacuating}|");
        //if ((flags & Citizen.Flags.Collapsed) != 0) txt.Append($"{Citizen.Flags.Collapsed}|");
        if ((flags & Citizen.Flags.Education1) != 0) txt.Append($"{Citizen.Flags.Education1}|");
        if ((flags & Citizen.Flags.Education2) != 0) txt.Append($"{Citizen.Flags.Education2}|");
        if ((flags & Citizen.Flags.Education3) != 0) txt.Append($"{Citizen.Flags.Education3}|");
        if ((flags & Citizen.Flags.NeedGoods) != 0) txt.Append($"{Citizen.Flags.NeedGoods}|");
        //if ((flags & Citizen.Flags.Original) != 0) txt.Append($"{Citizen.Flags.Original}|");
        //if ((flags & Citizen.Flags.CustomName) != 0) txt.Append($"{Citizen.Flags.CustomName}|");
        txt.Append($"W{((uint)flags >> 17) & 3}|"); // Wealth 0x60000
        txt.Append($"L{((uint)flags >> 19) & 3}|"); // Location 0x180000
        txt.Append($"U{((uint)flags >> 21) & 7}|"); // Unemployed 0xE00000
        txt.Append($"B{((uint)flags >> 24) & 3}|"); // BadHealth 0x3000000
        return txt.ToString();
    }
    */
}


/*
using(var w = new StreamWriter(path)) // it helps to set your writer like this: new StreamWriter(path, false, Encoding.UTF8)
{
    for( your loop )
    {
        var first = yourFnToGetFirst();
        var second = yourFnToGetSecond();
        var line = string.Format("{0},{1}", first, second);
        w.WriteLine(line);
        w.Flush();
    }
}
// don't forget to close the writer with w.Close(); w.Flush() is not needed
*/
