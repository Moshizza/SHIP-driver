using System.Collections.Generic;
using Apis.Salto.Ship;
using Apis.Salto.Ship.Model;
using Prysm.AppVision.Common;
using Prysm.AppVision.Data;

namespace AppDriverShip.Helpers;

internal static class SaltoSdkHelper
{
	internal static EditNewKeyBinaryRequestDesfire GetBinaryRequestDesfire(string keyId, string csn, ResponseKeyData responseKeyData, int desfireFileSize = 2048)
	{
		return new EditNewKeyBinaryRequestDesfire
		{
			KeyID = keyId,
			SaltoKeyData = responseKeyData.SaltoKeyData,
			ReturnKeyID = 1,
			DesfireStructure = new DesfireStructure
			{
				DesfireCardSerialNumber = csn,
				Files = new List<DesfireFile>
				{
					new DesfireFile
					{
						FileID = 1,
						FileSize = desfireFileSize
					}
				}
			}
		};
	}

	internal static EditNewKeyRequest GetNewKeyRequest(string keyId, string encoderId, SaltoKeyData saltoKeyData)
	{
		return new EditNewKeyRequest
		{
			KeyID = keyId,
			SaltoKeyData = saltoKeyData,
			EncoderID = encoderId
		};
	}

	internal static EditNewKeyBinaryResponse GetBinaryResponse(ShipApi shipApi, EditNewKeyBinaryRequest saltoRequest)
	{
		return shipApi.Encoder.GetBinaryDataForNewKey(saltoRequest).Result;
	}

	internal static string GetOperationDescription(int operationCode)
	{
		return operationCode switch
		{
			1 => RT.Get("Operation.OpeningOrClosing"), 
			8 => RT.Get("Operation.NewRenovationCodeForKey"), 
			15 => RT.Get("Operation.DoorOpened"), 
			16 => RT.Get("Operation.DoorOpenedInsideHandle"), 
			17 => RT.Get("Operation.DoorOpenedKey"), 
			18 => RT.Get("Operation.DoorOpenedKeyKeyboard"), 
			19 => RT.Get("Operation.DoorOpenedMultipleGuestKey"), 
			20 => RT.Get("Operation.DoorOpenedUniqueOpening"), 
			21 => RT.Get("Operation.DoorOpenedSwitch"), 
			22 => RT.Get("Operation.DoorOpenedMechanicalKey"), 
			23 => RT.Get("Operation.FirstDoubleCard"), 
			24 => RT.Get("Operation.DoorOpenedAfterSecondDoubleCard"), 
			25 => RT.Get("Operation.DoorOpenedPPD"), 
			26 => RT.Get("Operation.DoorOpenedKeyboard"), 
			27 => RT.Get("Operation.DoorOpenedSpareCard"), 
			28 => RT.Get("Operation.DoorOpenedOnlineCommand"), 
			29 => RT.Get("Operation.DoorMostProbablyOpenedKeyPin"), 
			30 => RT.Get("Operation.30"), 
			31 => RT.Get("Operation.31"), 
			32 => RT.Get("Operation.EndOfOfficeMode"), 
			33 => RT.Get("Operation.DoorClosedKey"), 
			34 => RT.Get("Operation.DoorClosedKeyKeyboard"), 
			35 => RT.Get("Operation.DoorClosed"), 
			36 => RT.Get("Operation.DoorClosedSwitch"), 
			37 => RT.Get("Operation.KeyInsertedEnergy"), 
			38 => RT.Get("Operation.KeyRemovedEnergy"), 
			39 => RT.Get("Operation.RoomPreparedEnergy"), 
			40 => RT.Get("Operation.StartOfPrivacy"), 
			41 => RT.Get("Operation.EndOfPrivacy"), 
			42 => RT.Get("Operation.DuressAlarm"), 
			43 => RT.Get("Operation.43"), 
			44 => RT.Get("Operation.44"), 
			45 => RT.Get("Operation.45"), 
			46 => RT.Get("Operation.46"), 
			47 => RT.Get("Operation.CommunicationWithReaderLost"), 
			48 => RT.Get("Operation.CommunicationWithReaderEstablished"), 
			49 => RT.Get("Operation.StartOfficeMode"), 
			50 => RT.Get("Operation.EndOfficeMode"), 
			51 => RT.Get("Operation.HotelGuestCancelled"), 
			52 => RT.Get("Operation.52"), 
			53 => RT.Get("Operation.53"), 
			54 => RT.Get("Operation.DoorProgrammedSpareKey"), 
			55 => RT.Get("Operation.HotelGuestKey"), 
			56 => RT.Get("Operation.StartOfficeModeOnline"), 
			57 => RT.Get("Operation.EndOfficeModeOnline"), 
			58 => RT.Get("Operation.StartForcedClosingOnline"), 
			59 => RT.Get("Operation.EndOfForcedClosingOnline"), 
			60 => RT.Get("Operation.AlarmIntrusionOnline"), 
			61 => RT.Get("Operation.AlarmTamperOnline"), 
			62 => RT.Get("Operation.DLO"), 
			63 => RT.Get("Operation.EndDLO"), 
			64 => RT.Get("Operation.EndIntrusion"), 
			65 => RT.Get("Operation.StartOfficeModeOnline"), 
			66 => RT.Get("Operation.EndOfficeModeOnline"), 
			67 => RT.Get("Operation.EndOfTamper"), 
			68 => RT.Get("Operation.AutomaticChange"), 
			69 => RT.Get("Operation.KeyUpdatedOutOfSiteMode"), 
			70 => RT.Get("Operation.ExpirationAutomaticallyExtendedOffline"), 
			71 => RT.Get("Operation.71"), 
			72 => RT.Get("Operation.OnlinePeripheralUpdated"), 
			73 => RT.Get("Operation.73"), 
			74 => RT.Get("Operation.74"), 
			75 => RT.Get("Operation.75"), 
			76 => RT.Get("Operation.KeyUpdatedOnline"), 
			77 => RT.Get("Operation.77"), 
			78 => RT.Get("Operation.KeyDeletedOnline"), 
			79 => RT.Get("Operation.CommunicationLost"), 
			80 => RT.Get("Operation.CommunicationEstablished"), 
			81 => RT.Get("Operation.OpeningNotAllowedKeyNoActivated"), 
			82 => RT.Get("Operation.OpeningNotAllowedKeyExpired"), 
			83 => RT.Get("Operation.OpeningNotAllowedKeyOutOfDate"), 
			84 => RT.Get("Operation.OpeningNotAllowedInThisDoor"), 
			85 => RT.Get("Operation.OpeningNotAllowedOutOfTime"), 
			86 => RT.Get("Operation.86"), 
			87 => RT.Get("Operation.OpeningNotAllowedOverridePrivacy"), 
			88 => RT.Get("Operation.OpeningNotAllowedOldHotelGuestKey"), 
			89 => RT.Get("Operation.OpeningNotAllowedHotelGuestKeyCancelled"), 
			90 => RT.Get("Operation.OpeningNotAllowedAntipassback"), 
			91 => RT.Get("Operation.OpeningNotAllowedSecondCardNotPresented"), 
			92 => RT.Get("Operation.OpeningNotAllowedNoAssociatedAuthorization"), 
			93 => RT.Get("Operation.OpeningNotAllowedInvalidPIN"), 
			94 => RT.Get("Operation.94"), 
			95 => RT.Get("Operation.OpeningNotAllowedEmergencyState"), 
			96 => RT.Get("Operation.OpeningNotAllowedKeyCancelled"), 
			97 => RT.Get("Operation.OpeningNotAllowedOpeningKeyAlreadyUsed"), 
			98 => RT.Get("Operation.OpeningNotAllowedOldRenovationNumber"), 
			99 => RT.Get("Operation.OpeningNotAllowedKeyNotCompletelyUpdatedOnline"), 
			100 => RT.Get("Operation.OpeningNotAllowedRunOutOfBattery"), 
			101 => RT.Get("Operation.OpeningNotAllowedUnableToAuditKey"), 
			102 => RT.Get("Operation.OpeningNotAllowedLockerOccupancyTimeout"), 
			103 => RT.Get("Operation.OpeningNotAllowedDeniedByHost"), 
			104 => RT.Get("Operation.BlacklistedKeyDeleted"), 
			105 => RT.Get("Operation.105"), 
			106 => RT.Get("Operation.106"), 
			107 => RT.Get("Operation.OpeningNotAllowedKeyDataManipulated"), 
			108 => RT.Get("Operation.108"), 
			109 => RT.Get("Operation.109"), 
			110 => RT.Get("Operation.110"), 
			111 => RT.Get("Operation.ClosingNotAllowedDoorInEmergencyState"), 
			112 => RT.Get("Operation.NewRenovationCode"), 
			113 => RT.Get("Operation.PPDConnection"), 
			114 => RT.Get("Operation.TimeModifiedDaylight"), 
			115 => RT.Get("Operation.CommunicationSaltoLost"), 
			116 => RT.Get("Operation.IncorrectClockValue"), 
			117 => RT.Get("Operation.RFLockDateTime"), 
			118 => RT.Get("Operation.RFLockUpdated"), 
			119 => RT.Get("Operation.UnableToPerformOpenCloseOperation"), 
			120 => RT.Get("Operation.120"), 
			_ => RT.Get($"Operation.{operationCode}"), 
		};
	}

	internal static string GetOperationGroup(int operationCode)
	{
		switch (operationCode)
		{
		case 15:
		case 16:
		case 17:
		case 18:
		case 19:
		case 20:
		case 21:
		case 22:
		case 23:
		case 24:
		case 25:
		case 26:
		case 27:
		case 28:
		case 29:
		case 33:
		case 34:
		case 35:
		case 36:
			return RT.Get("OperationGroup.OpeningsAndClosings");
		case 37:
		case 38:
		case 39:
		case 40:
		case 41:
			return RT.Get("OperationGroup.Actions");
		case 32:
		case 47:
		case 48:
		case 49:
		case 50:
		case 51:
		case 54:
		case 55:
		case 65:
		case 66:
		case 68:
		case 79:
		case 80:
			return RT.Get("OperationGroup.DoorStatusChanges");
		case 56:
		case 57:
		case 58:
		case 59:
		case 72:
		case 118:
			return RT.Get("OperationGroup.OnlineCommandsFromHost");
		case 8:
		case 69:
		case 70:
		case 76:
		case 78:
		case 99:
		case 104:
			return RT.Get("OperationGroup.KeyModifications");
		case 81:
		case 82:
		case 83:
		case 84:
		case 85:
		case 87:
		case 88:
		case 89:
		case 90:
		case 91:
		case 92:
		case 93:
		case 95:
		case 96:
		case 97:
		case 98:
		case 100:
		case 101:
		case 102:
		case 103:
		case 107:
		case 111:
			return RT.Get("OperationGroup.Rejections");
		case 112:
		case 113:
		case 114:
		case 115:
		case 116:
		case 117:
		case 119:
			return RT.Get("OperationGroup.Maintenance");
		case 42:
		case 60:
		case 61:
		case 62:
		case 63:
		case 64:
		case 67:
			return RT.Get("OperationGroup.AlarmsAndWarnings");
		default:
			return "";
		}
	}

	internal static ReaderStatus GetReaderStatus(int operation, out string info)
	{
		switch (operation)
		{
		case 1:
			info = RT.Get("Reader.Ok");
			return (ReaderStatus)40;
		case 15:
			info = RT.Get("Reader.DoorOpened");
			return (ReaderStatus)40;
		case 16:
			info = RT.Get("Reader.DoorOpenedInsideHandle");
			return (ReaderStatus)10;
		case 17:
			info = RT.Get("Reader.DoorOpenedKey");
			return (ReaderStatus)10;
		case 18:
			info = RT.Get("Reader.DoorOpenedKeyKeyboard");
			return (ReaderStatus)10;
		case 21:
			info = RT.Get("Reader.DoorOpenedSwitch");
			return (ReaderStatus)10;
		case 22:
			info = RT.Get("Reader.DoorOpenedMechanicalKey");
			return (ReaderStatus)10;
		case 23:
			info = RT.Get("Reader.DoorOpenedFirstCardRead");
			return (ReaderStatus)10;
		case 24:
			info = RT.Get("Reader.DoorOpenedSecondCard");
			return (ReaderStatus)10;
		case 25:
			info = RT.Get("Reader.DoorOpenedPPD");
			return (ReaderStatus)10;
		case 26:
			info = RT.Get("Reader.DoorOpenedKeyboard");
			return (ReaderStatus)10;
		case 27:
			info = RT.Get("Reader.DoorOpenedSpareCard");
			return (ReaderStatus)10;
		case 28:
			info = RT.Get("Reader.DoorOpenedOnlineCommand");
			return (ReaderStatus)10;
		case 29:
			info = RT.Get("Reader.DoorProbablyOpened");
			return (ReaderStatus)10;
		case 33:
			info = RT.Get("Reader.DoorCloseKey");
			return (ReaderStatus)10;
		case 34:
			info = RT.Get("Reader.DoorClosedKeyboard");
			return (ReaderStatus)10;
		case 35:
			info = RT.Get("Reader.DoorSwitch");
			return (ReaderStatus)10;
		case 36:
			info = RT.Get("Reader.DoorOpenedKeyKeyboard");
			return (ReaderStatus)10;
		case 37:
		case 38:
		case 39:
		case 40:
		case 41:
			info = $"Action> {operation}";
			return (ReaderStatus)0;
		case 32:
		case 47:
		case 48:
		case 49:
		case 50:
		case 51:
		case 54:
		case 55:
		case 65:
		case 66:
		case 68:
		case 79:
		case 80:
			info = $"DoorStatusChanged> {operation}";
			return (ReaderStatus)0;
		case 56:
		case 57:
		case 58:
		case 59:
		case 72:
		case 118:
			info = $"Online commands from host> {operation}";
			return (ReaderStatus)0;
		case 8:
		case 69:
		case 70:
		case 76:
		case 78:
		case 99:
		case 104:
			info = $"Key modification> {operation}";
			return (ReaderStatus)0;
		case 81:
			info = RT.Get("Reader.OpeningNotAllowedKeyNotActivated");
			return (ReaderStatus)50;
		case 82:
			info = RT.Get("Reader.OpeningNotAllowedKeyExpired");
			return (ReaderStatus)56;
		case 83:
			info = RT.Get("Reader.OpeningNotAllowedKeyOutOfDate");
			return (ReaderStatus)56;
		case 84:
			info = RT.Get("Reader.OpeningNotAllowedForThisDoor");
			return (ReaderStatus)50;
		case 85:
			info = RT.Get("Reader.OpeningNotAllowedOutOfTime");
			return (ReaderStatus)58;
		case 87:
			info = RT.Get("Reader.OpeningNotAllowedOverridePrivacy");
			return (ReaderStatus)50;
		case 88:
			info = RT.Get("Reader.OpeningNotAllowedOldHotelGuestKey");
			return (ReaderStatus)50;
		case 89:
			info = RT.Get("Reader.OpeningNotAllowedGuestKeyCancelled");
			return (ReaderStatus)57;
		case 90:
			info = RT.Get("Reader.OpeningNotAllowedAntipassback");
			return (ReaderStatus)50;
		case 91:
			info = RT.Get("Reader.OpeningNotAllowedSecondCardNotPresented");
			return (ReaderStatus)50;
		case 92:
			info = RT.Get("Reader.OpeningNotAllowedNoAssociatedAuthorization");
			return (ReaderStatus)50;
		case 93:
			info = RT.Get("Reader.OpeningNotAllowedInvalidPIN");
			return (ReaderStatus)50;
		case 95:
			info = RT.Get("Reader.OpeningNotAllowedEmmergencyState");
			return (ReaderStatus)50;
		case 96:
			info = RT.Get("Reader.OpeningNotAllowedKeyCancelled");
			return (ReaderStatus)57;
		case 97:
			info = RT.Get("Reader.OpeningNotAllowedAlreadyUsed");
			return (ReaderStatus)50;
		case 98:
			info = RT.Get("Reader.OpeningNotAllowedOldRenovationNumber");
			return (ReaderStatus)50;
		case 100:
			info = RT.Get("Reader.OpeningNotAllowedOutOfBattery");
			return (ReaderStatus)50;
		case 101:
			info = RT.Get("Reader.OpeningNotAllowedAuditOnKey");
			return (ReaderStatus)50;
		case 102:
			info = RT.Get("Reader.OpeningNotAllowedLockerOccupancyTimeout");
			return (ReaderStatus)50;
		case 103:
			info = RT.Get("Reader.OpeningNotAllowedDeniedByHost");
			return (ReaderStatus)50;
		case 107:
			info = RT.Get("Reader.OpeningNotAllowedDataManipulated");
			return (ReaderStatus)60;
		case 111:
			info = RT.Get("Reader.ClosingNotAllowedEmergencyState");
			return (ReaderStatus)50;
		case 112:
		case 113:
		case 114:
		case 115:
		case 116:
		case 117:
		case 119:
			info = "Maintenance";
			return (ReaderStatus)0;
		case 42:
		case 60:
		case 61:
		case 62:
		case 63:
		case 64:
		case 67:
			info = "Alarms and warnings";
			return (ReaderStatus)0;
		default:
			info = "Operation not found";
			return (ReaderStatus)0;
		}
	}
}
