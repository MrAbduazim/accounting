import clr
import os
from System import Byte, Int32, Array
from accounting.settings import BASE_DIR

# Add reference to the DLL
dll_path = os.path.join(BASE_DIR, 'RFID_SDK', 'UHFReader288.dll')
clr.AddReference(dll_path)
from UHF import UHFReader  # Adjust the namespace and class name if necessary

# Create an instance of UHFReader
reader = UHFReader()

# Call OpenNetPort
def OpenNtePort(port, ip_address, com_addr):
    result = reader.OpenNetPort(port, ip_address, com_addr)
    return result

def Inventory_G2():
    # Parameters for Inventory_G2
    com_adr = Byte(0)
    q_value = Byte(1)
    session = Byte(0)
    mask_mem = Byte(2)
    mask_adr = Array[Byte]([0] * 2)
    mask_len = Byte(0)
    mask_data = Array[Byte]([0] * 256)
    mask_flag = Byte(0)
    adr_tid = Byte(0)
    len_tid = Byte(6)
    tid_flag = Byte(0)
    target = Byte(0)
    in_ant = Byte(0x80)
    scantime = Byte(10)
    fast_flag = Byte(0)
    epc_list = Array[Byte]([0] * 20000)
    ant = Byte(0)  # If the byte array is needed, it can be Array[Byte]([0])
    total_len = Int32(0)
    card_num = Int32(0)

    # Call Inventory_G2
    result = reader.Inventory_G2(
        com_adr, q_value, session, mask_mem, mask_adr, mask_len,
        mask_data, mask_flag, adr_tid, len_tid, tid_flag, target,
        in_ant, scantime, fast_flag, epc_list, ant, total_len, card_num
    )
    print("Inventory_G2 Result:", result)
    print("Total Length:", total_len)
    print("Card Number:", card_num)

    return result
