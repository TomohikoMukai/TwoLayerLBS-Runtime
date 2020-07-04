# TwoLayerLBS-Runtime
本コードは、[Two-Layer Sparse Compression of Dense-Weight Blend Skinning](http://graphics.cs.uh.edu/ble/papers/2013s-dwc/ "TwoLayerLBS paper")のランタイム部分の実装のサンプルです。

## 動作環境
- Unity 2019.4 （2019以降が必要です）
- Windows 10環境でのみ動作を確認しています。MacOS環境では動作していません。

## データ処理の流れ
1. Maya 2018にて簡単なクロスシミュレーションを生成。頂点数は6561。
    - cloth.ma
2. [SSDS](https://github.com/TomohikoMukai/ssds "SSDS")を用いてジョイントアニメーションに変換。ジョイント数は50、各頂点のインフルーエンスは最大12に設定。
    - cloth.maおよび clothanim.fbx
3. 二段階スキンモデルに変換。一段階目の仮想ジョイント数は860、仮想ジョイントあたりのインフルーエンスは8に設定。二段階目の頂点当たりインフルーエンスは4に設定。
    - 一段階目のスキンウェイトはVirtualIndex.csvとVirtualWeight.csvに保存
    - 一段階目のスキンウェイトはcloth.fbxアセットに設定するとともに、VertexlIndex.csvとVertexlWeight.csvにも保存
    - オリジナルのアルゴリズムに対して、ウェイト正則化（非負制約およびアフィン制約）を追加するなど少数の拡張を実施
4. ランタイムのスキニング計算。一段階目の計算はCompute Shaderで実行（TwoLayerLBS.compute）、二段階目はVertex Shaderで実行（TwoLayerLBS.shader）。

## 更新履歴
- [2020.7.4] 公開