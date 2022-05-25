function deleteWebhook(instance, callback, webhookGuid, webhookName) {
    if (!webhookGuid) {
        throw new Error('webhookGuid is required');
    }

    if (confirm('Delete webhook ' + webhookName + '?')) {
        instance.invokeMethodAsync(callback, webhookGuid);
    }
}