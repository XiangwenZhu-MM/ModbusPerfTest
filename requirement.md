### SCADA Requirements: Performance Monitoring & Data Integrity

**1. Device-Level Performance Metrics**
The SCADA system shall generate three specific metrics for every Modbus TCP frame to evaluate performance:

* 
**QueueDuration_ms**: Measures the internal software delay between a task being scheduled and the driver pulling it from the queue for execution.


* 
**DeviceResponseTime_ms**: Measures the physical round-trip time () from the driver to the PLC and back.


* 
**ActualSamplingInterval_ms**: Represents the total processing deviation (the sum of internal and external delays).



**2. System-Level Health Metrics**
The system must provide a rolling 60-second calculation of the following data acquisition "engine" metrics:

* 
**Ingress_TPM**: The total workload demand (new tasks created).


* 
**Egress_TPM**: The actual system capacity or "speed limit" (tasks completed).


* 
**SaturationIndex**: The ratio of Ingress to Egress; values > 100% indicate a critical state where the system cannot keep up.


* 
**Dropped_TPM**: The count of tasks deleted due to a full queue, indicating active data loss.



**3. Stale Quality Requirements**
To maintain data integrity, the system must implement a "Stale Quality" state for tags.

* 
**Trigger Condition**: A tag is marked as STALE if the time since the last success () exceeds the threshold (), defined as two times the requested scan interval.


* **Behavioral Constraints**:
* 
**Retain Last Known Value (LKV)**: The HMI must display the last valid number but change its visual state (e.g., gray out).


* 
**Retain Original Timestamp**: The system must preserve the timestamp of the actual last update to show how "old" the data is.


* 
**Quality Code Modification**: Only the Quality bit of the tag is changed (e.g., from Good to Stale/Uncertain).




* 
**Automatic Recovery**: The Quality must be set back to "Good" immediately upon the arrival of a new valid packet.



---

Would you like me to create a separate summary of the formulas used in these requirements?