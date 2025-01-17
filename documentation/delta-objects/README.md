# Delta objects

Delta Kusto computes a delta between two Kusto *sources* (either an actual ADX database or a KQL script) and produces a *delta* KQL script.

## Model

Delta Kusto first builds a *database model* for each source, i.e. the *current* and the *target*, and compute the delta between those two *database models*.

## Inputs

The inputs for a model is the actual source:  either an ADX database or a KQL script (or scripts).

In the case of scripts, Delta Kusto doesn't accept every commands related to an object.  For instance, only `.create function` and `.create-or-alter function` are accepted ; `.alter function`, `.alter function docstring` etc. aren't accepted.

This is both for simplicity but also because Delta Kusto doesn't enforce a script order.  In order to compute a model with `.alter function` or `.alter function docstring`, we would need to know which one is called first.

## Objects

This page documents how those delta are computed on different objects:

* [Functions](functions.md)
* [Ingestion Mappings](ingestion-mappings.md)
* Policies
    * [Auto Delete Policy](policies/auto-delete.md)
    * [Caching Policy](policies/caching.md)
    * [Ingestion Batching Policy](policies/ingestion-batching.md)
    * [Merge Policy](policies/merge.md)
    * [Retention Policy](policies/retention.md)
    * [Sharding Policy](policies/sharding.md)
    * [Update Policy](policies/update.md)
* [Tables](tables.md)
