from django.shortcuts import render
from django.views.generic import ListView
from .models import Catalog
# Create your views here.

class CatalogList(ListView):
    context_object_name = 'elements'
    template_name = 'catalog/catalog_list.html'

    def get_context_data(self, **kwargs):
        context = super().get_context_data(**kwargs)
        context["cells"] = Catalog._meta.fields
