from django.urls import path
from .views import AddRFIDTags

urlpatterns = [
    path('add-rfid-tag', AddRFIDTags, name='add_rfid_tags'),
]