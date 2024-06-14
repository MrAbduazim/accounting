from django.shortcuts import render
from inventory.index import OpenNetPort, OpenComPort, Inventory_G2

# Create your views here.
def open_com_port(request):
    result = OpenComPort(port=6, com_addr=0, baud=5)

    inventory_result = Inventory_G2()

    return render(request, "inventory/add_rfid_tag.html", {"tags": inventory_result})
