from django.db import models

# Create your models here.
class Catalog(models.Model):
    description = models.CharField(max_length=250)
    deletion_mark = models.BooleanField(default=False)

    def __str__(self):
        return self.description