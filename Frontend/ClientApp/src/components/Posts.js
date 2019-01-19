import React, { Component } from 'react';
import { Grid, GridItem } from 'react-masonry-infinite-scroll';
import Utils from "../Utils";
import './Posts.css';

export class Posts extends Component {
  displayName = Posts.name

  constructor(props) {
    super(props);
    this.state = { posts: [], loading: true, hasMore: false };

    this.loadMore = this.loadMore.bind(this);
    this.imageReady = this.imageReady.bind(this);

    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + props.match.params.blogname)
      .then(response => response.json())
      .then(data => {
        this.setState({ posts: data, loading: false, itemsLeft: data.length === 50 ? 1 : 0 });
      });
  }
  
  loadMore() {
    const [lastPost] = this.state.posts.slice(-1);
    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + this.props.match.params.blogname + "?after=" + lastPost.Id)
      .then(response => response.json())
      .then(data => {
        this.setState(state => ({
          posts: state.posts.concat(data),
          itemsLeft: data.length === 50 ? 1 : 0
        }));
      });
  }

  imageReady() {
    this.masonryGrid.imageLoaded();
  }

  notifyReadyState() {
    
  }

  renderPostsTable(posts) {
    return (
      <Grid columnWidth={260} itemsLeft={this.state.itemsLeft} loadMore={this.loadMore} ref={(child) => { this.masonryGrid = child; }} notifyReadyState={this.notifyReadyState} 
        scrollThreshold={0}>
        {posts.map(post =>
          <GridItem key={post.Id}>
            {(!post.Photos || post.Photos.length === 0) && 
              <span>No photo</span>
            }
            {(post.Photos && post.Photos.length !== 0) &&
              <div className="photo-post"><a href={ "/post/" + post.Blogname + "/" + post.Id}> 
                <img src={Utils.GetBigPhotoUrl(post)} width="250" onLoad={this.imageReady} onError={this.imageReady} alt=""/>
              </a></div>
            }
          </GridItem>
        )}
      </Grid>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : this.renderPostsTable(this.state.posts);

    return (
      <div>
        <h1>{this.props.match.params.blogname}</h1>
        {contents}
      </div>
    );
  }
}
