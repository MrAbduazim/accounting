from django.shortcuts import render
from inventory.index import OpenNetPort, Inventory_G2

# Create your views here.
def AddRFIDTags(request):
    result = OpenNetPort(port=27010, ip_address="192.168.0.25", com_addr=0)

    inventory_result = Inventory_G2()

    return render(request, "inventory/add_rfid_tag.html", {"tags": inventory_result})
