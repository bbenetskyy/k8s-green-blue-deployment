# K8s Blue/Green deployment strategy:
Blue/Green strategy is used to minimize downtime for production services. 

Its typically achieved by keeping two identical production environments - **Prod**(**Green**) and **Stage**(**Blue**). Only one of which is serving live production traffic. 

When a new release has to be rolled out, its first deployed in the **Stage** environment. When all the testing is done, the live traffic is moved to **Stage** which becomes the **Prod** environment, and current **Prod** environment becomes the **Stage**. There is added benefit of a fast rollback by just changing route if new issue is found with live traffic.