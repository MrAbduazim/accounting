from django.db import models
from application_object.models import Catalog

# Create your models here.
class Unit(Catalog):
    pass

class Nomenclature(Catalog):
    unit_field = models.ForeignKey(Unit, on_delete=models.SET_NULL, null=True)


class NomenclatureTag(Catalog):
    nomenclature_field = models.ForeignKey(Nomenclature, on_delete=models.SET_NULL, null=True)
    rfid_tag = models.CharField(max_length=250)