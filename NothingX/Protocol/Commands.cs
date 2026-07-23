namespace NothingX.Protocol;

/// <summary>
/// All Nothing earbuds protocol command constants.
/// Reverse-engineered from com.nothing.base.protocol.constant.ProtocolConstant
/// </summary>
public static class Commands
{
    /// <summary>Query commands — read device state (base 0xC000)</summary>
    public static class Query
    {
        public const int BASE = 0xC000;
        public const int GET_PROTOCOL_VERSION = 0xC001;
        public const int GET_FIND_EAR_STATE = 0xC002;
        public const int GET_REMOTE_MTU = 0xC003;
        public const int GET_REMOTE_VID = 0xC004;
        public const int GET_REMOTE_DEVICE_IDENTIFICATION = 0xC005;
        public const int GET_REMOTE_CONFIGURATION = 0xC006;
        public const int GET_REMOTE_BATTERY_LEVEL = 0xC007;
        public const int GET_UPGRADE_CAPABILITY = 0xC008;
        public const int GET_SUPPORTED_GESTURE = 0xC009;
        public const int GET_EARPHONE_STATUS = 0xC00A;
        public const int GET_REMOTE_EXTRA_VERSION_CODE = 0xC00B;
        public const int GET_REMOTE_COLOR_ID = 0xC00C;
        public const int GET_SUPPORTED_FEATURE = 0xC00D;
        public const int GET_EXTRA_FEATURE_STATUS = 0xC00E;
        public const int GET_EQ_ID = 0xC00F;
        public const int GET_HIGH_VOLUME_GAIN_LEVEL = 0xC010;
        public const int GET_AUTO_POWER_OFF_TIME = 0xC011;
        public const int GET_EARPHONE_CONNECTED_STATUS = 0xC013;
        public const int GET_VOLUME = 0xC014;
        public const int GET_CODEC_CAPABILITY = 0xC015;
        public const int GET_MANUFACTURE = 0xC016;
        public const int GET_BOX_LED_COLOR = 0xC017;
        public const int GET_KEY_CONFIGURATION = 0xC018;
        public const int GET_DEVICE_WORKING_STATUS = 0xC019;
        public const int GET_DEVICE_MODEL = 0xC01C;
        public const int GET_NOISE_REDUCTION_CONFIGURATION = 0xC01D;
        public const int GET_HIGH_QUALITY_AUDIO = 0xC01D; // Alias: LDAC status (confirmed via btsnoop)
        public const int GET_CURRENT_NOISE_REDUCTION = 0xC01E;
        public const int GET_EQ_MODE = 0xC01F;
        public const int GET_PERSONALIZED_ANC = 0xC020;
        public const int GET_PERSONALIZED_NOISE_VALUE = 0xC021;
        public const int GET_MIMI_ENABLE = 0xC022;
        public const int GET_MIMI_INTENSITY = 0xC023;
        public const int GET_MIMI_PRESET_ID = 0xC024;
        public const int GET_MIMI_FITTING_TECH_LEVEL = 0xC025;
        public const int GET_3D_MODE = 0xC026;
        public const int GET_DUAL_ENABLE = 0xC027;
        public const int GET_DUAL_DEVICE_LIST = 0xC028;
        public const int GET_LHDC_COMMANDS = 0xC029;
        public const int GET_SUPPORTED_NOTIFICATION = 0xC03D;
        public const int GET_REGISTERED_NOTIFICATION = 0xC03E;
        public const int GET_HOST_UTC_TIME = 0xC03F;
        public const int GET_HOST_LAG_MODE = 0xC041;
        public const int GET_HOST_VERSION_DEVICE = 0xC042;
        public const int GET_ADAPTIVE_EQ_MODE = 0xC043;
        public const int GET_SIMPLE_CUSTOM_EQ = 0xC044; // Was GET_CUSTOM_EQ_VALUE
        public const int GET_ADVANCE_CUSTOM_EQ_MODE = 0xC042;
        public const int GET_ADVANCE_CUSTOM_EQ_VALUE = 0xC050;
        public const int GET_BASS_BOOST = 0xC04E;
        public const int GET_SPATIAL_AUDIO = 0xC04F;
        public const int GET_ANC_FIR_MODE = 0xC051;
        public const int GET_BASS_ENHANCER_MODE = 0xC053;
        public const int GET_SMART_FREE_MODE = 0xC054;
        public const int GET_SMART_ANC_MODE = 0xC055;
        public const int GET_LE_SWITCH = 0xC056;
        public const int GET_SYSTEM_AUDIO = 0xC057;
        public const int GET_HEADTRACK_START = 0xC058;
        public const int GET_LE_AUDIO_CONNECT_MODE = 0xC059;
        public const int GET_BOX_VERSION = 0xC05C;
        public const int GET_MUTUALLY_EXCLUSIVE = 0xC062;
        public const int GET_DETAIL_ENHANCEMENT = 0xC069;
        public const int GET_SCENARIO_MODE = 0xC071;
    }

    /// <summary>Set commands — write device settings (base 0xF000)</summary>
    public static class Set
    {
        public const int BASE = 0xF000;
        public const int SET_PROTOCOL_ACTIVATED = 0xF001;
        public const int SET_WHERE_AM_I = 0xF002;
        public const int SET_KEY_CONFIGURATION = 0xF003;
        public const int SET_EXTRA_FEATURE_STATUS = 0xF004;
        public const int SET_EQ_STATUS = 0xF005;
        public const int SET_HIGH_VOLUME_GAIN_LEVEL = 0xF006;
        public const int SET_UTC_TIME = 0xF007;
        public const int SET_AUTO_POWER_OFF_TIME = 0xF00B;
        public const int SET_BOX_LED_COLOR = 0xF009;
        public const int SET_NOISE_REDUCTION_CONFIGURATION = 0xF00E;
        public const int SET_CURRENT_NOISE_REDUCTION = 0xF00F;
        public const int SET_EQ_MODE = 0xF010;
        public const int RESTORE_FACTORY_SETTING = 0xF011;
        public const int REGISTER_NOTIFICATION = 0xF012;
        public const int UNREGISTER_NOTIFICATION = 0xF013;
        public const int SET_LAG_MODE = 0xF040;
        public const int SET_CUSTOM_EQ = 0xF015;
        public const int SET_ADAPTIVE_EQ = 0xF016;
        public const int SET_DUAL_ENABLE = 0xF01A;
        public const int SET_DUAL_DEVICE = 0xF01B;
        // Added for EQ Sync Fix
        public const int SET_SIMPLE_CUSTOM_EQ = 0xF041;
        public const int SET_ADVANCE_CUSTOM_EQ_MODE = 0xF042;
        public const int SET_ADVANCE_CUSTOM_EQ_VALUE = 0xF043;
        public const int SET_BASS_BOOST = 0xF031;
        public const int SET_HIGH_QUALITY_AUDIO = 0xF01C; // LDAC toggle (confirmed via btsnoop: payload 02=LDAC, 00=AAC)
        public const int SET_SPATIAL_AUDIO = 0xF052;
        public const int SET_BASS_ENHANCER = 0xF051; // Bass Enhancer (confirmed via btsnoop: byte0=enable, byte1=level*5)
        public const int SET_BASS_ENHANCER_MODE = 0xF057;
        public const int SET_SMART_FREE_MODE = 0xF038;
        public const int SET_SMART_ANC_MODE = 0xF039;
        public const int SET_LE_SWITCH_MODEL = 0xF03A;
        public const int SET_SYSTEM_AUDIO = 0xF03B;
        public const int SET_ESSENTIAL_SPACE_STATUS = 0xF042;
        public const int SET_DETAIL_ENHANCEMENT = 0xF049;
        public const int SET_SCENARIO_MODE = 0xF055;
        public const int OTA_FIND_NEW_VERSION = 0xF024;
        public const int OTA_DOWNLOADED_NEW_VERSION = 0xF025;
        public const int OTA_STOP_ERROR = 0xF026;
    }

    /// <summary>Notification commands — events pushed from earbuds (base 0xE000)</summary>
    public static class Notification
    {
        public const int BASE = 0xE000;
        public const int EVENT_BATTERY_CHANGED = 0xE001;
        public const int EVENT_DEVICE_STATUS_CHANGED = 0xE002;
        public const int EVENT_NOISE_REDUCTION_LEVEL_CHANGED = 0xE003;
        public const int EVENT_GAME_MODE_CHANGED = 0xE005;
        public const int EVENT_DUAL_DEVICE_SWITCH_STATE = 0xE006;
        public const int EVENT_WORKING_STATUS_CHANGE = 0xE009;
        public const int EVENT_LED_COLOR_SYNC_NOTIFICATION = 0xE00B;
        public const int EVENT_PERSONALIZE_SYNC_NOTIFICATION = 0xE00C;
        public const int EVENT_TIP_FIT_RESULT = 0xE00D;
        public const int EVENT_DUAL_DEVICE_CONNECT_STATE = 0xE00E;
        public const int NOTIFY_DISCONNECT_PROFILE = 0xE00F;
        public const int NOTIFY_REQUEST_START_OTA = 0xE010;
        public const int NOTIFY_REQUEST_STOP_OTA = 0xE011;
        public const int EVENT_MAGIC_BUTTON = 0xE014;
        public const int EVENT_HEAD_TRACK = 0xE015;
        public const int EVENT_LE_AUDIO_CONNECT = 0xE016;
        public const int EVENT_RECORDING = 0xE018;
    }

    /// <summary>Debug commands (base 0xFC00)</summary>
    public static class Debug
    {
        public const int BASE = 0xFC00;
        public const int ENTER_TEST_MODE = 0xFC01;
        public const int PARAMETER_NEGOTIATION = 0xFC02;
        public const int GET_FILE_LIST = 0xFC03;
        public const int QUERY_SINGLE_FILE_INFO = 0xFC04;
        public const int REQUEST_SINGLE_FILE_INFO = 0xFC05;
        public const int DEVICE_SEND_DATA = 0xFC06;
        public const int EXIT_TEST_MODE = 0xFC07;
        public const int CHANGE_LEVEL = 0xFC08;
        public const int GET_DEBUG_INFO = 0xFC09;
    }

    /// <summary>Get the human-readable name for a command ID</summary>
    public static string GetName(int commandId)
    {
        return commandId switch
        {
            Query.GET_REMOTE_BATTERY_LEVEL => "GetBattery",
            Query.GET_CURRENT_NOISE_REDUCTION => "GetAncMode",
            Query.GET_EQ_MODE => "GetEqMode",
            Query.GET_SIMPLE_CUSTOM_EQ => "GetSimpleCustomEq",
            Query.GET_ADVANCE_CUSTOM_EQ_VALUE => "GetAdvancedCustomEq",
            Query.GET_ADVANCE_CUSTOM_EQ_MODE => "GetAdvancedCustomEqMode",
            Query.GET_PROTOCOL_VERSION => "GetProtocolVersion",
            Query.GET_EARPHONE_STATUS => "GetEarphoneStatus",
            Query.GET_SUPPORTED_FEATURE => "GetSupportedFeature",
            Query.GET_KEY_CONFIGURATION => "GetGestures",
            Query.GET_SPATIAL_AUDIO => "GetSpatialAudio",
            Query.GET_HOST_LAG_MODE => "GetLowLatency",
            Set.SET_PROTOCOL_ACTIVATED => "Activate",
            Set.SET_CURRENT_NOISE_REDUCTION => "SetAncMode",
            Set.SET_EQ_MODE => "SetEqMode",
            Set.SET_CUSTOM_EQ => "SetCustomEq",
            Set.SET_WHERE_AM_I => "FindEarbuds",
            Set.SET_LAG_MODE => "SetLowLatency",
            Set.SET_SPATIAL_AUDIO => "SetSpatialAudio",
            Set.SET_KEY_CONFIGURATION => "SetGestures",
            Set.REGISTER_NOTIFICATION => "RegisterNotification",
            Notification.EVENT_BATTERY_CHANGED => "BatteryChanged",
            Notification.EVENT_NOISE_REDUCTION_LEVEL_CHANGED => "AncChanged",
            Notification.EVENT_DEVICE_STATUS_CHANGED => "DeviceStatusChanged",
            _ => $"0x{commandId:X4}"
        };
    }
}
