# Azure Disk CSI plugin on Kubernetes

## Prerequisite
An azure stroage account

## Deploy
1. Fill in storage account information in secret.env, and environment variables in attacher.yaml
2. Deploy Azure disk CSI plugin, run following
```
kubectl create ns csi-plugins-azuredisk
kubectl --namespace=csi-plugins-azuredisk create secret generic csi-azuredisk-secret --from-env-file=secret.env
kubectl --namespace=csi-plugins-azuredisk create -f .
```
3. Deploy demo pod, run following
```
kubectl create ns demo
kubectl --namespace=demo create -f pod
kubectl --namespace=demo get pods
kubectl --namespace=demo get pvc
```

## Cleanup
```
kubectl delete ns demo

# pending pvc clean up, after demo namespace deleted
kubectl delete ns csi-plugins-azuredisk

kubectl delete clusterrole external-attacher-runner
kubectl delete clusterrole external-provisioner-runner
kubectl delete clusterrole csi-plugin-azuredisk-runner

kubectl delete clusterrolebinding csi-attacher-role
kubectl delete clusterrolebinding csi-provisioner-role
kubectl delete clusterrolebinding csi-plugin-azuredisk-role

kubectl delete storageclasses azuredisk-csi

```
