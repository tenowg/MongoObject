# Understanding Vector Similarity Metrics in MongoDB

When implementing Vector Search, selecting the correct similarity metric is the most critical decision for ensuring accurate results. A similarity metric defines the mathematical formula MongoDB uses to compare vector embeddings and calculate search scores.

MongoDB Atlas Vector Search natively supports three metric types: Cosine, Dot Product, and Euclidean.
## 1. Cosine Similarity (cosine)  
### Mathematical Focus
Measures the angle between two vector arrows in high-dimensional space, completely ignoring their length (magnitude).
### How Scores are Calculated
MongoDB converts the traditional cosine range (\(-1\) to \(1\)) into a normalized similarity score between 0.0 and 1.0 using the formula:

<div align="center">

 $$(\text{Score}=\frac{\text{Cosine\ Similarity}+1}{2})$$

 </div>

  * **Perfect Match Score:** 1.0 (Vectors point in the exact same direction).
  * **Total Opposite Score:** 0.0 (Vectors point in exactly opposite directions).

### Primary Use Cases
  * **Text and Natural Language Processing (NLP):** Finding similarities in sentences, documents, and paragraphs.
  * **Semantic Search:** Comparing short query strings against long blocks of text.

### Developer Insights
Because text length and word choice cause vector magnitudes to vary wildly, Cosine is the industry standard for text. It prevents long documents from penalizing or inflating scores unfairly, ensuring that "Craig" and a long paragraph about "Craig" yield clean, human-readable scoring gaps (e.g., 0.7 to 0.9).

## 2. Dot Product (dotProduct)
### Mathematical Focus
Multiplies the matching components of two vectors and sums them up. It evaluates both the angle and the length (magnitude) of the vectors.

### How Scores are Calculated
If your embeddings are unit-normalized (pre-scaled to a vector length of exactly 1.0), Dot Product behaves exactly like Cosine:

<div align="center">

$$(\text{Score}=\frac{\text{Dot Product}+1}{2})$$

</div>

* **Perfect Match Score:** 1.0 (Assuming unit-normalized vectors)
* ***Warning:*** If vectors are unnormalized, scores can scale infinitely and exceed 1.0.

### Primary Use Cases
* **Standard AI Embedding Models:** Perfect for models that natively output normalized vectors, such as OpenAI (text-embedding-3), Cohere, or Hugging Face Transformers.
* **Large-Scale Production Systems:** Recommendation systems where query performance is critical.
### Developer Insights
Dot Product is mathematically faster to compute than Cosine because it skips the complex square-root division steps required to calculate vector lengths. If you enforce or guarantee that your application uses normalized embeddings, switching from cosine to dotProduct provides a noticeable performance boost at scale while producing identical search rankings.
## 3. Euclidean Distance (euclidean)
### Mathematical Focus
Measures the straight-line distance ("as the crow flies") between the endpoints of two vector arrows.
### How Scores are Calculated
True Euclidean distance is 0.0 for a perfect match and grows infinitely as vectors diverge. Because search engines require higher scores to mean "better matches," MongoDB converts this distance into a similarity fraction:

<div align="center">

$$(\text{Score}=\frac{1}{1+\text{Euclidean\ Distance}^{2}})$$

</div>

* **Perfect Match Score:** 1.0 (Distance is exactly 0.0)
* **Poor Match Score:** Approaches 0.0 as the straight-line distance grows.

### Primary Use Cases
* **Fixed-Length Numeric Data:** Audio signals, sensor telemetry, and spatial/geometric data.
* **Computer Vision:** Comparing image embeddings where absolute spatial variance dictates identity.
### Developer Insights
Avoid using Euclidean for natural language text search. Because text blocks vary in length, their vector lengths differ drastically, causing the straight-line distance between them to stretch. This scaling anomaly compresses all text search results into an unnaturally narrow and unreadable scoring band (often hovering tightly between 0.50 and 0.51), making application filtering logic incredibly difficult to write.

## Metric Cheat Sheet
|Metric|Primary Data Type|Math Focus|Perfect Score|Best For|
|------|-----------------|----------|-------------|--------|
|cosine|Text / Sentences|Direction / Angle|1.0|Default text search, asymmetric text lengths.|
|dotProduct|Text / Features|Direction + Length|1.0 (Normalized)|Maximum performance with OpenAI/Cohere embeddings.|
|euclidean|Images / Sensors|Absolute Distance|1.0|Spatial tracking, visual assets, fixed-length numbers.|