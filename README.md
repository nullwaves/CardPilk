# CardPilk
A tool for cart-based management of TCGPlayer inventory & pricing. Initial data must be imported from TCGPlayer CSVs.
Prices are NOT live, they are only pulled from imports. Always download new prices before repricing.

<img style="max-width:600px; padding: 5px;" src="https://bucket-weed.com/xbb/wELU0/SOHATAVO35.png/raw">

## Features/TODO List

+ [✔️] Import CSV files exported from TCGplayer
+ [✔️] Filter inventory by In Stock, Product Line, Set, Condition
+ [✔️] Cards grouped together with variants
+ [✔️] Repricer tool
+ [✔️] Create "Carts" with inventory changes
+ [✔️] Export "Carts" for staging changes into TCGplayer
+ [✔️] Export CSV files for TCGplayer with updated prices
+ [✔️] Scryfall Integration (Card Images)
+ [✔️] Lorcast Integration (Card Images)
+ [❌] Local Image Caching

## Workflows

### Repricing

This process is the general workflow for using CardPilk to bulk update TCGplayer prices with a fixed percentage of TCGplayers rates and the ability to set a minimum price.

1. Export inventory from TCGplayer's Pricing tab
1. Import into CardPilk
1. Adjust repricer setting, then run.
1. Export repricer batch
1. Import to TCGplayer's Staged inventory

### Buying Cards

This process demonstrates how CardPilk can be used to handle adding large quantities of cards to your TCGplayer inventory.

NOTE: No data is included with this repository. You will first need to import any games, sets, or categories from TCGplayer. You can export this data from TCGplayer's Pricing tab in the Seller Dashboard.

1. Add Cards to cart, then save.
1. Go to Cart History
1. Export the cart, upload to TCGplayer's Staged inventory

## Add'l Screenshots

<img style="max-width:600px; padding: 5px;" src="https://bucket-weed.com/xbb/wELU0/lequpEzE44.png/raw">
<img style="max-width:600px; padding: 5px;" src="https://bucket-weed.com/xbb/wELU0/qufUCiXE15.png/raw">
<img style="max-width:600px; padding: 5px;" src="https://bucket-weed.com/xbb/wELU0/RIxecUbI00.png/raw">