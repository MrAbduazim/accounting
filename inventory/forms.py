from django import forms
from .models import RFIDTag


class RFIDTagForm(forms.ModelForm):

    class Meta:
        fields = "__all__"